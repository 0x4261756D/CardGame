using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.GameConstants;
using CardGameUtils.Replay;
using CardGameUtils.Structs.Duel;
using static CardGameUtils.Functions;

namespace CardGameCore;

class DuelCore : Core
{
	private static GameConstantsElectricBoogaloo.State _state = GameConstantsElectricBoogaloo.State.UNINITIALIZED;
	public static GameConstantsElectricBoogaloo.State State
	{
		get => _state;
		set
		{
			Log($"STATE: {_state} -> {value}");
			_state = value;
		}
	}

	public int multiplicativeDamageModifier = 1;

	public static uint UIDCount, CardActionUIDCount;
	public Player[] players;
	public static NetworkStream?[] playerStreams = [];
	public static Random rnd = new(Program.seed);
	public const int HASH_LEN = 96;
	private readonly CardAction AbilityUseActionDescription;
	private readonly CardAction CastActionDescription;
	private readonly CardAction CreatureMoveActionDescription;
	public int playersConnected;
	public int turn;
	public int turnPlayer, initPlayer;
	public int nextMomentumIncreaseIndex;
	public int? markedZone;
	public int momentumBase = GameConstantsElectricBoogaloo.START_MOMENTUM;
	public bool rewardClaimed;
	public CoreConfig.DuelConfig config;

	private readonly Dictionary<uint, List<Trigger>> castTriggers = [];
	private readonly Dictionary<uint, List<LocationBasedTargetingTrigger>> genericCastTriggers = [];
	private readonly Dictionary<uint, List<TokenCreationTrigger>> tokenCreationTriggers = [];
	private readonly Dictionary<uint, List<LocationBasedTargetingTrigger>> genericEnterFieldTriggers = [];
	private readonly Dictionary<uint, List<Trigger>> revelationTriggers = [];
	private readonly Dictionary<uint, List<Trigger>> victoriousTriggers = [];
	private readonly Dictionary<uint, List<CreatureTargetingTrigger>> genericVictoriousTriggers = [];
	private readonly Dictionary<uint, List<CreatureTargetingTrigger>> attackTriggers = [];
	private readonly Dictionary<uint, List<CreatureTargetingTrigger>> deathTriggers = [];
	private readonly Dictionary<uint, List<CreatureTargetingTrigger>> genericDeathTriggers = [];
	private readonly Dictionary<uint, List<LocationBasedTrigger>> youDiscardTriggers = [];
	private readonly Dictionary<uint, List<Trigger>> discardTriggers = [];
	private readonly Dictionary<uint, List<StateReachedTrigger>> stateReachedTriggers = [];
	private readonly List<StateReachedTrigger> alwaysActiveStateReachedTriggers = [];
	private readonly Dictionary<uint, LingeringEffectList> lingeringEffects = [];
	private readonly Dictionary<uint, LingeringEffectList> locationTemporaryLingeringEffects = [];
	private readonly Dictionary<GameConstantsElectricBoogaloo.State, LingeringEffectList> stateTemporaryLingeringEffects = [];
	private readonly LingeringEffectList alwaysActiveLingeringEffects;
	private readonly Dictionary<uint, List<ActivatedEffectInfo>> activatedEffects = [];
	private readonly Dictionary<uint, List<Trigger>> dealsDamageTriggers = [];

	private class LingeringEffectList : IEnumerable<LingeringEffectInfo>
	{
		private readonly List<LingeringEffectInfo> items = [];
		private readonly DuelCore core;
		public LingeringEffectList(DuelCore core)
		{
			this.core = core;
			core.EvaluateLingeringEffects();
		}

		public void Add(LingeringEffectInfo info)
		{
			items.Add(info);
			core.EvaluateLingeringEffects();
		}

		public void Remove(LingeringEffectInfo info)
		{
			_ = items.Remove(info);
			core.EvaluateLingeringEffects();
		}

		public void RemoveAll(Predicate<LingeringEffectInfo> condition)
		{
			if(items.RemoveAll(condition) > 0)
			{
				core.EvaluateLingeringEffects();
			}
		}

		public IEnumerator<LingeringEffectInfo> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return items.GetEnumerator();
		}
	}

	public DuelCore(CoreConfig.DuelConfig config, int port) : base(port)
	{
		AbilityUseActionDescription = new(uid: CardActionUIDCount, description: "Use");
		CardActionUIDCount += 1;
		CastActionDescription = new(uid: CardActionUIDCount, description: "Cast");
		CardActionUIDCount += 1;
		CreatureMoveActionDescription = new(uid: CardActionUIDCount, description: "Move");
		CardActionUIDCount += 1;
		this.config = config;
		RegisterScriptingFunctions();
		alwaysActiveLingeringEffects = new(this);
		players = new Player[config.players.Length];
		playerStreams = new NetworkStream[config.players.Length];
		for(int i = 0; i < players.Length; i++)
		{
			Log("Player created. ID: " + config.players[i].id);
			Deck deck = new();
			PlayerClass playerClass = Enum.Parse<PlayerClass>(config.players[i].decklist[0]);
			string abilityString = config.players[i].decklist[1];
			if(!abilityString.StartsWith('#'))
			{
				Log($"Player {config.players[i].name} has no ability, {abilityString} is no suitable ability");
				return;
			}
			Spell ability = (Spell)CreateBasicCard(Type.GetType(CardnameToFilename(abilityString[1..]))!, i);
			string questString = config.players[i].decklist[2];
			if(!questString.StartsWith('|'))
			{
				Log($"Player {config.players[i].name} has no quest, {questString} is no suitable ability");
				return;
			}
			Quest quest = (Quest)CreateBasicCard(Type.GetType(CardnameToFilename(questString[1..]))!, i);
			foreach(string cardString in config.players[i].decklist[3..])
			{
				if(string.IsNullOrWhiteSpace(cardString))
				{
					continue;
				}
				Log($"Creating {cardString}");
				deck.Add(CreateBasicCard(Type.GetType(CardnameToFilename(cardString))!, i));
			}
			players[i] = new Player(config.players[i], i, deck, playerClass, ability, quest);
		}
	}

	public void RegisterScriptingFunctions()
	{
		Card.RemoveLingeringEffect = RemoveLingeringEffectImpl;
		Card.SetDamageMultiplier = SetDamageMultiplierImpl;
		Card.GetDamageMultiplier = GetDamageMultiplierImpl;
		Card.RegisterCastTrigger = RegisterCastTriggerImpl;
		Card.RegisterGenericCastTrigger = RegisterGenericCastTriggerImpl;
		Card.RegisterGenericEntersFieldTrigger = RegisterGenericEntersFieldTriggerImpl;
		Card.RegisterRevelationTrigger = RegisterRevelationTriggerImpl;
		Card.RegisterYouDiscardTrigger = RegisterYouDiscardTriggerImpl;
		Card.RegisterDiscardTrigger = RegisterDiscardTriggerImpl;
		Card.RegisterStateReachedTrigger = RegisterStateReachedTriggerImpl;
		Card.RegisterVictoriousTrigger = RegisterVictoriousTriggerImpl;
		Card.RegisterGenericVictoriousTrigger = RegisterGenericVictoriousTriggerImpl;
		Card.RegisterAttackTrigger = RegisterAttackTriggerImpl;
		Card.RegisterDeathTrigger = RegisterDeathTriggerImpl;
		Card.RegisterGenericDeathTrigger = RegisterGenericDeathTriggerImpl;
		Card.RegisterLingeringEffect = RegisterLingeringEffectImpl;
		Card.RegisterLocationTemporaryLingeringEffect = RegisterLocationTemporaryLingeringEffectImpl;
		Card.RegisterStateTemporaryLingeringEffect = RegisterStateTemporaryLingeringEffectImpl;
		Card.RegisterActivatedEffect = RegisterActivatedEffectImpl;
		Card.RegisterTokenCreationTrigger = RegisterTokenCreationTriggerImpl;
		Card.GetGrave = GetGraveImpl;
		Card.GetField = GetFieldImpl;
		Card.GetFieldUsed = GetFieldUsedImpl;
		Card.GetHand = GetHandImpl;
		Card.SelectCards = SelectCardsImpl;
		Card.Discard = DiscardImpl;
		Card.DiscardAmount = DiscardAmountImpl;
		Card.CreateTokenOnField = CreateTokenOnFieldImpl;
		Card.CreateToken = CreateTokenImpl;
		Card.CreateTokenCopyOnField = CreateTokenCopyOnFieldImpl;
		Card.CreateTokenCopy = CreateTokenCopyImpl;
		Card.GetDiscardCountXTurnsAgo = GetDiscardCountXTurnsAgoImpl;
		Card.GetDamageDealtXTurnsAgo = GetDamageDealtXTurnsAgoImpl;
		Card.GetSpellDamageDealtXTurnsAgo = GetSpellDamageDealtXTurnsAgoImpl;
		Card.GetBrittleDeathCountXTurnsAgo = GetBrittleDeathCountXTurnsAgoImpl;
		Card.GetDeathCountXTurnsAgo = GetDeathCountXTurnsAgoImpl;
		Card.PlayerChangeLife = PlayerChangeLifeImpl;
		Card.PlayerChangeMomentum = PlayerChangeMomentumImpl;
		Card.Cast = CastImpl;
		Card.Draw = DrawImpl;
		Card.Destroy = DestroyImpl;
		Card.AskYesNo = AskYesNoImpl;
		Card.GetIgniteDamage = GetIgniteDamageImpl;
		Card.ChangeIgniteDamage = ChangeIgniteDamageImpl;
		Card.ChangeIgniteDamageTemporary = ChangeIgniteDamageTemporaryImpl;
		Card.GetTurn = GetTurnImpl;
		Card.GetPlayerLife = GetPlayerLifeImpl;
		Card.PayLife = PayLifeImpl;
		Card.Gather = GatherImpl;
		Card.Move = MoveImpl;
		Card.SelectZone = SelectZoneImpl;
		Card.MoveToHand = MoveToHandImpl;
		Card.MoveToField = MoveToFieldImpl;
		Card.GetCastCount = GetCastCountImpl;
		Card.ReturnCardsToDeck = ReturnCardsToDeckImpl;
		Card.Reveal = RevealImpl;
		Card.GetDiscardable = GetDiscardableImpl;
		Card.RefreshAbility = ResetAbilityImpl;
		Card.RegisterDealsDamageTrigger = RegisterDealsDamageTriggerImpl;
		Card.CreatureChangeLife = CreatureChangeLifeImpl;
		Card.CreatureChangePower = CreatureChangePowerImpl;
	}

	public override void Init(PipeStream? pipeStream)
	{
		listener.Start();
		pipeStream?.WriteByte(42);
		pipeStream?.Close();
		Log("Listening", severity: LogSeverity.Warning);
		HandleNetworking();
		foreach(NetworkStream? stream in playerStreams)
		{
			stream?.Dispose();
		}
		listener.Stop();
	}

	private static Card CreateBasicCard([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, int controller)
	{
		Card card = (Card)Activator.CreateInstance(type)!;
		card.BaseController = controller;
		card.ResetToBaseState();
		card.Init();
		card.isInitialized = true;
		return card;
	}

	public override void HandleNetworking()
	{
		while(playersConnected < players.Length)
		{
			if(listener.Pending())
			{
				ConnectNewPlayer();
			}
		}
		while(true)
		{
			if(HandleGameLogic())
			{
				Log("Game ends by game logic");
				break;
			}
			if(HandlePlayerActions())
			{
				Log("Game ends by player action");
				break;
			}
		}
	}

	private void ConnectNewPlayer()
	{
		DateTime t = DateTime.Now;
		Log($"{t.ToLongTimeString()}:{t.Millisecond} New Player {playersConnected}/{players.Length}");
		NetworkStream stream = listener.AcceptTcpClient().GetStream();
		byte[] buf = new byte[256];
		int len = stream.Read(buf, 0, HASH_LEN);
		if(len != HASH_LEN)
		{
			Log($"len was {len} but expected {HASH_LEN}\n-------------------\n{Encoding.UTF8.GetString(buf)}", severity: LogSeverity.Error);
			stream.Close();
			return;
		}
		string id = Encoding.UTF8.GetString(buf, 0, len);
		bool foundPlayer = false;
		for(int i = 0; i < players.Length; i++)
		{
			if(playerStreams[i] == null)
			{
				Log($"Player id: {players[i].id} ({players[i].id.Length}), found {id} ({id.Length}) | {players[i].id == id}");
				if(players[i].id == id)
				{
					playersConnected++;
					foundPlayer = true;
					playerStreams[i] = stream;
					stream.WriteByte((byte)i);
				}
			}
		}
		if(!foundPlayer)
		{
			Log("Found no player", severity: LogSeverity.Error);
			stream.Close();
		}
	}

	private void EvaluateLingeringEffects()
	{
		if(State == GameConstantsElectricBoogaloo.State.UNINITIALIZED)
		{
			return;
		}
		foreach(Player player in players)
		{
			player.ClearCardModifications();
		}
		_ = stateTemporaryLingeringEffects.Remove(State);
		SortedList<int, LingeringEffectInfo> infos = [];
		foreach(LingeringEffectList list in stateTemporaryLingeringEffects.Values)
		{
			foreach(LingeringEffectInfo info in list)
			{
				if(IsInLocation(info.influenceLocation, info.referrer.Location))
				{
					if(info.timestamp == 0)
					{
						info.timestamp = LingeringEffectInfo.timestampCounter;
						LingeringEffectInfo.timestampCounter++;
					}
					infos.Add(info.timestamp, info);
				}
				else
				{
					info.timestamp = 0;
				}
			}
		}
		foreach(Player player in players)
		{
			foreach(Card card in player.hand.GetAll())
			{
				if(lingeringEffects.TryGetValue(card.uid, out LingeringEffectList? handInfos))
				{
					foreach(LingeringEffectInfo info in handInfos)
					{
						if(IsInLocation(info.influenceLocation, card.Location))
						{
							if(info.timestamp == 0)
							{
								info.timestamp = LingeringEffectInfo.timestampCounter;
								LingeringEffectInfo.timestampCounter++;
							}
							infos.Add(info.timestamp, info);
						}
						else
						{
							info.timestamp = 0;
						}
					}
				}
				if(locationTemporaryLingeringEffects.TryGetValue(card.uid, out LingeringEffectList? handTempInfos))
				{
					foreach(LingeringEffectInfo info in handTempInfos)
					{
						if(IsInLocation(info.influenceLocation, card.Location))
						{
							if(info.timestamp == 0)
							{
								info.timestamp = LingeringEffectInfo.timestampCounter;
								LingeringEffectInfo.timestampCounter++;
							}
							infos.Add(info.timestamp, info);
						}
						else
						{
							info.timestamp = 0;
						}
					}
				}
			}
			foreach(Card card in player.field.GetUsed())
			{
				if(lingeringEffects.TryGetValue(card.uid, out LingeringEffectList? fieldInfos))
				{
					foreach(LingeringEffectInfo info in fieldInfos)
					{
						if(IsInLocation(info.influenceLocation, card.Location))
						{
							if(info.timestamp == 0)
							{
								info.timestamp = LingeringEffectInfo.timestampCounter;
								LingeringEffectInfo.timestampCounter++;
							}
							infos.Add(info.timestamp, info);
						}
						else
						{
							info.timestamp = 0;
						}
					}
				}
				if(locationTemporaryLingeringEffects.TryGetValue(card.uid, out LingeringEffectList? fieldTempInfos))
				{
					foreach(LingeringEffectInfo info in fieldTempInfos)
					{
						if(IsInLocation(info.influenceLocation, card.Location))
						{
							if(info.timestamp == 0)
							{
								info.timestamp = LingeringEffectInfo.timestampCounter;
								LingeringEffectInfo.timestampCounter++;
							}
							infos.Add(info.timestamp, info);
						}
						else
						{
							info.timestamp = 0;
						}
					}
				}
			}
		}
		foreach(Player player in players)
		{
			if(lingeringEffects.TryGetValue(player.quest.uid, out LingeringEffectList? questInfos))
			{
				foreach(LingeringEffectInfo info in questInfos)
				{
					if(info.timestamp == 0)
					{
						info.timestamp = LingeringEffectInfo.timestampCounter;
						LingeringEffectInfo.timestampCounter++;
					}
					infos.Add(info.timestamp, info);
				}
			}
			if(locationTemporaryLingeringEffects.TryGetValue(player.quest.uid, out LingeringEffectList? questTempInfos))
			{
				foreach(LingeringEffectInfo info in questTempInfos)
				{
					if(info.timestamp == 0)
					{
						info.timestamp = LingeringEffectInfo.timestampCounter;
						LingeringEffectInfo.timestampCounter++;
					}
					infos.Add(info.timestamp, info);
				}
			}
		}
		foreach(KeyValuePair<int, LingeringEffectInfo> info in infos)
		{
			info.Value.effect(info.Value.referrer);
			CheckQuestReward(false);
		}
		foreach(LingeringEffectInfo info in alwaysActiveLingeringEffects)
		{
			info.effect(info.referrer);
			CheckQuestReward(false);
		}
		foreach(Player player in players)
		{
			for(int i = 0; i < GameConstantsElectricBoogaloo.FIELD_SIZE; i++)
			{
				Creature? card = player.field.GetByPosition(i);
				if(card != null && card.Life <= 0)
				{
					DestroyImpl(card);
				}
			}
		}
	}

	private void ProcessCreatureTargetingTriggers(Dictionary<uint, List<CreatureTargetingTrigger>> triggers, Creature target, Location location, uint uid)
	{
		if(triggers.TryGetValue(uid, out List<CreatureTargetingTrigger>? value))
		{
			foreach(CreatureTargetingTrigger trigger in value)
			{
				EvaluateLingeringEffects();
				if(IsInLocation(trigger.influenceLocation, location) && trigger.condition(target))
				{
					trigger.effect(target);
					CheckQuestReward();
				}
			}
			EvaluateLingeringEffects();
		}
	}
	public void ProcessLocationBasedTargetingTriggers(Dictionary<uint, List<LocationBasedTargetingTrigger>> triggers, Card target, uint uid)
	{
		if(triggers.TryGetValue(uid, out List<LocationBasedTargetingTrigger>? matchingTriggers))
		{
			foreach(LocationBasedTargetingTrigger trigger in matchingTriggers)
			{
				EvaluateLingeringEffects();
				if(trigger.condition(target))
				{
					trigger.effect(target);
					CheckQuestReward();
				}
			}
			EvaluateLingeringEffects();
		}
	}
	public void ProcessLocationBasedTriggers(Dictionary<uint, List<LocationBasedTrigger>> triggers, Location location, uint uid)
	{
		if(triggers.TryGetValue(uid, out List<LocationBasedTrigger>? matchingTriggers))
		{
			for(int i = 0; i < matchingTriggers.Count; i++)
			{
				LocationBasedTrigger trigger = matchingTriggers[i];
				EvaluateLingeringEffects();
				if(IsInLocation(trigger.influenceLocation, location) && trigger.condition())
				{
					trigger.effect();
					CheckQuestReward();
				}
			}
			EvaluateLingeringEffects();
		}
	}
	private void ProcessStateReachedTriggers()
	{
		foreach(StateReachedTrigger trigger in alwaysActiveStateReachedTriggers)
		{
			EvaluateLingeringEffects();
			if(trigger.state == State && trigger.condition())
			{
				trigger.effect();
				trigger.wasTriggered = true;
				CheckQuestReward();
			}
		}
		_ = alwaysActiveStateReachedTriggers.RemoveAll(card => card.oneshot && card.wasTriggered);
		EvaluateLingeringEffects();
		if(stateReachedTriggers.Count > 0)
		{
			foreach(Player player in players)
			{
				if(stateReachedTriggers.TryGetValue(player.quest.uid, out List<StateReachedTrigger>? questTriggers))
				{
					foreach(StateReachedTrigger trigger in questTriggers)
					{
						EvaluateLingeringEffects();
						if(trigger.state == State && trigger.condition())
						{
							trigger.effect();
							trigger.wasTriggered = true;
							CheckQuestReward();
						}
					}
					EvaluateLingeringEffects();
					_ = questTriggers.RemoveAll(trigger => trigger.oneshot && trigger.wasTriggered);
				}

				foreach(Card card in player.hand.GetAll())
				{
					if(stateReachedTriggers.TryGetValue(card.uid, out List<StateReachedTrigger>? handTriggers))
					{
						foreach(StateReachedTrigger trigger in handTriggers)
						{
							EvaluateLingeringEffects();
							if(trigger.state == State && IsInLocation(trigger.influenceLocation, Location.Hand) && trigger.condition())
							{
								trigger.effect();
								trigger.wasTriggered = true;
								CheckQuestReward();
							}
							else
							{
								trigger.wasTriggered = false;
							}
						}
						_ = handTriggers.RemoveAll(x => x.oneshot && x.wasTriggered);
						EvaluateLingeringEffects();
					}
				}
				foreach(Card? card in player.field.GetAll())
				{
					if(card != null && stateReachedTriggers.TryGetValue(card.uid, out List<StateReachedTrigger>? fieldTriggers))
					{
						foreach(StateReachedTrigger trigger in fieldTriggers)
						{
							EvaluateLingeringEffects();
							if(trigger.state == State && IsInLocation(trigger.influenceLocation, Location.Field) && trigger.condition())
							{
								trigger.effect();
								trigger.wasTriggered = true;
								CheckQuestReward();
							}
							else
							{
								trigger.wasTriggered = false;
							}
						}

						_ = fieldTriggers.RemoveAll(x => x.oneshot && x.wasTriggered);
						EvaluateLingeringEffects();
					}
				}
			}
		}
	}
	public void ProcessTokenCreationTriggers(Dictionary<uint, List<TokenCreationTrigger>> triggers, Creature token, Card source, uint uid)
	{
		if(triggers.TryGetValue(uid, out List<TokenCreationTrigger>? matchintTriggers))
		{
			foreach(TokenCreationTrigger trigger in matchintTriggers)
			{
				EvaluateLingeringEffects();
				if(trigger.condition(token: token, source: source))
				{
					trigger.effect(token: token, source: source);
					CheckQuestReward();
				}
			}
			EvaluateLingeringEffects();
		}
	}
	public void ProcessTriggers(Dictionary<uint, List<Trigger>> triggers, uint uid)
	{
		if(triggers.TryGetValue(uid, out List<Trigger>? matchingTriggers))
		{
			foreach(Trigger trigger in matchingTriggers)
			{
				EvaluateLingeringEffects();
				if(trigger.condition())
				{
					trigger.effect();
					CheckQuestReward();
				}
			}
			EvaluateLingeringEffects();
		}
	}

	public void CheckQuestReward(bool shouldEvaluateLingeringEffects = true)
	{
		if(rewardClaimed)
		{
			return;
		}
		if(shouldEvaluateLingeringEffects)
		{
			EvaluateLingeringEffects();
		}
		if(rewardClaimed)
		{
			return;
		}
		foreach(Player player in players)
		{
			if(player.quest.Progress >= player.quest.Goal)
			{
				rewardClaimed = true;
				player.quest.Reward();
				player.quest.Text += "\nREWARD CLAIMED";
				break;
			}
		}
	}
	private bool HandleGameLogic()
	{
		while(!State.HasFlag(GameConstantsElectricBoogaloo.State.InitGained))
		{
			if(State != GameConstantsElectricBoogaloo.State.UNINITIALIZED)
			{
				EvaluateLingeringEffects();
				for(int i = 0; i < players.Length; i++)
				{
					CheckIfLost(i);
					if(players[i].life <= 0)
					{
						return true;
					}
				}
			}
			switch(State)
			{
				case GameConstantsElectricBoogaloo.State.UNINITIALIZED:
				{
					foreach(Player player in players)
					{
						if(!config.noshuffle)
						{
							player.deck.Shuffle();
						}
						player.Draw(GameConstantsElectricBoogaloo.START_HAND_SIZE);
						player.momentum = momentumBase;
						player.life = GameConstantsElectricBoogaloo.START_LIFE;
						player.discardCounts.Add(0);
						player.dealtDamages.Add(0);
						player.dealtSpellDamages.Add(0);
						player.brittleDeathCounts.Add(0);
						player.deathCounts.Add(0);
					}
					turnPlayer = rnd.Next(100) / 50;
					initPlayer = turnPlayer;
					turn = 0;
					SendFieldUpdates();
					// Mulligan
					for(int i = 0; i < players.Length; i++)
					{
						if(AskYesNoImpl(player: i, question: "Mulligan?"))
						{
							Card[] cards = SelectCardsCustom(i, "Select cards to mulligan", players[i].hand.GetAll(), (x) => true);
							foreach(Card card in cards)
							{
								players[i].hand.Remove(card);
								AddCardToLocation(card, Location.Deck);
							}
							players[i].deck.Shuffle();
							players[i].Draw(cards.Length);
							Log("Done with mulligan, sending updates now");
							SendFieldUpdates();
						}
					}
					State = GameConstantsElectricBoogaloo.State.TurnStart;
				}
				break;
				case GameConstantsElectricBoogaloo.State.TurnStart:
				{
					foreach(Player player in players)
					{
						player.abilityUsable = true;
						player.momentum = momentumBase;
						player.castCounts.Clear();
						player.Draw(1);
					}
					foreach(KeyValuePair<uint, List<ActivatedEffectInfo>> lists in activatedEffects)
					{
						foreach(ActivatedEffectInfo list in lists.Value)
						{
							list.uses = 0;
						}
					}
					initPlayer = turnPlayer;
					ProcessStateReachedTriggers();
					State = GameConstantsElectricBoogaloo.State.MainInitGained;
				}
				break;
				case GameConstantsElectricBoogaloo.State.MainInitGained:
					break;
				case GameConstantsElectricBoogaloo.State.MainActionTaken:
				{
					initPlayer = 1 - initPlayer;
					players[initPlayer].passed = false;
					State = GameConstantsElectricBoogaloo.State.MainInitGained;
				}
				break;
				case GameConstantsElectricBoogaloo.State.BattleStart:
				{
					// The marked zone is relative to the 0th player
					// If player 1 is turnplayer it is FIELD_SIZE-1, the rightmost zone
					markedZone = turnPlayer * (GameConstantsElectricBoogaloo.FIELD_SIZE - 1);
					initPlayer = turnPlayer;
					foreach(Player player in players)
					{
						player.passed = false;
					}
					ProcessStateReachedTriggers();
					State = GameConstantsElectricBoogaloo.State.BattleInitGained;
				}
				break;
				case GameConstantsElectricBoogaloo.State.BattleInitGained:
					break;
				case GameConstantsElectricBoogaloo.State.BattleActionTaken:
				{
					initPlayer = 1 - initPlayer;
					players[initPlayer].passed = false;
					State = GameConstantsElectricBoogaloo.State.BattleInitGained;
				}
				break;
				case GameConstantsElectricBoogaloo.State.DamageCalc:
				{
					if(markedZone != null)
					{
						Creature? card0 = players[0].field.GetByPosition(GetMarkedZoneForPlayer(0));
						Creature? card1 = players[1].field.GetByPosition(GetMarkedZoneForPlayer(1));
						if(card0 != null)
						{
							ProcessCreatureTargetingTriggers(triggers: attackTriggers, uid: card0.uid, location: Location.Field, target: card0);
						}
						if(card1 != null)
						{
							ProcessCreatureTargetingTriggers(triggers: attackTriggers, uid: card1.uid, location: Location.Field, target: card1);
						}
						if(card0 == null)
						{
							if(card1 != null)
							{
								// Deal damage to player
								DealDamage(player: 0, amount: card1.Power, source: card1);
								if(players[0].life <= 0)
								{
									return true;
								}
							}
						}
						else
						{
							if(card1 == null)
							{
								// Deal damage to player
								DealDamage(player: 1, amount: card0.Power, source: card0);
								if(players[1].life <= 0)
								{
									return true;
								}
							}
							else
							{
								//Creature Combat
								if(card0.Keywords.ContainsKey(Keyword.Mighty) ^ card1.Keywords.ContainsKey(Keyword.Mighty))
								{
									if(card0.Keywords.ContainsKey(Keyword.Mighty))
									{
										int excessDamage = card0.Power * multiplicativeDamageModifier - card1.Life;
										if(excessDamage > 0) { DealDamage(player: 1, amount: excessDamage, source: card0, alreadyDealtWithDamageModifiers: true); }
									}
									else
									{
										int excessDamage = card1.Power * multiplicativeDamageModifier - card0.Life;
										if(excessDamage > 0) { DealDamage(player: 0, amount: excessDamage, source: card1, alreadyDealtWithDamageModifiers: true); }
									}
								}
								CreatureChangeLifeImpl(target: card0, amount: -card1.Power, source: card1);
								CreatureChangeLifeImpl(target: card1, amount: -card0.Power, source: card0);
								if(card0.Location != Location.Field && card1.Location == Location.Field)
								{
									ProcessTriggers(victoriousTriggers, card1.uid);
									for(int playerIndex = 0; playerIndex < players.Length; playerIndex++)
									{
										ProcessCreatureTargetingTriggers(genericVictoriousTriggers, target: card1, location: Location.Quest, uid: players[playerIndex].quest.uid);
									}
									foreach(Creature creature in CardUtils.GetBothFieldsUsed())
									{
										ProcessCreatureTargetingTriggers(genericVictoriousTriggers, target: card1, location: Location.Field, uid: creature.uid);
									}
								}
								if(card1.Location != Location.Field && card0.Location == Location.Field)
								{
									for(int playerIndex = 0; playerIndex < players.Length; playerIndex++)
									{
										ProcessCreatureTargetingTriggers(genericVictoriousTriggers, target: card0, location: Location.Quest, uid: players[playerIndex].quest.uid);
									}
									ProcessTriggers(victoriousTriggers, card0.uid);
									foreach(Creature creature in CardUtils.GetBothFieldsUsed())
									{
										ProcessCreatureTargetingTriggers(genericVictoriousTriggers, target: card0, location: Location.Field, uid: creature.uid);
									}
								}
							}
						}
					}
					foreach(Player player in players)
					{
						player.passed = false;
					}
					MarkNextZoneOrContinue();
				}
				break;
				case GameConstantsElectricBoogaloo.State.TurnEnd:
				{
					foreach(Player player in players)
					{
						for(int i = 0; i < GameConstantsElectricBoogaloo.FIELD_SIZE; i++)
						{
							Creature? creature = player.field.GetByPosition(i);
							if(creature != null)
							{
								if(creature.Keywords.ContainsKey(Keyword.Brittle))
								{
									DestroyImpl(creature);
								}
								if(creature.Keywords.ContainsKey(Keyword.Decaying))
								{
									RegisterLocationTemporaryLingeringEffectImpl(info: LingeringEffectInfo.Create(effect: (target) => target.Life -= 1, referrer: creature));
									if(creature.Life == 0 && creature.Location == Location.Field)
									{
										DestroyImpl(creature);
									}
								}
							}
						}
						player.discardCounts.Add(0);
						player.dealtDamages.Add(0);
						player.dealtSpellDamages.Add(0);
						player.brittleDeathCounts.Add(0);
						player.deathCounts.Add(0);
					}
					ProcessStateReachedTriggers();
					for(int i = 0; i < players.Length; i++)
					{
						Player player = players[i];
						int toDeckCount = player.hand.Count - GameConstantsElectricBoogaloo.MAX_HAND_SIZE;
						if(toDeckCount > 0)
						{
							SendPacketToPlayer(new SToC_Content.select_cards(new
							(
								amount: (uint)toDeckCount,
								cards: Card.ToStruct(player.hand.GetAll()),
								description: "Select cards to shuffle into you deck for hand size"
							)), i);
							List<uint> toDeck = ReceivePacketFromPlayer<CToS_Content.select_cards>(i).value.uids;
							foreach(uint uid in toDeck)
							{
								Card card = player.hand.GetByUID(uid);
								player.hand.Remove(card);
								player.deck.Add(card);
							}
							player.deck.Shuffle();
							SendFieldUpdates();
						}
					}
					turnPlayer = 1 - turnPlayer;
					turn++;
					if(nextMomentumIncreaseIndex < GameConstantsElectricBoogaloo.MOMENTUM_INCREMENT_TURNS.Length && GameConstantsElectricBoogaloo.MOMENTUM_INCREMENT_TURNS[nextMomentumIncreaseIndex] == turn)
					{
						momentumBase++;
						nextMomentumIncreaseIndex++;
					}
					State = GameConstantsElectricBoogaloo.State.TurnStart;
				}
				break;
				default:
					throw new NotImplementedException(State.ToString());
			}
			SendFieldUpdates();
		}
		return false;
	}

	private void CheckIfLost(int player)
	{
		if(players[player].life <= 0)
		{
			SendFieldUpdates();
			SendPacketToPlayer(new SToC_Content.game_result(new(GameResult.Lost)), player);
			SendPacketToPlayer(new SToC_Content.game_result(new(GameResult.Won)), 1 - player);
		}
	}

	public void RegisterGenericVictoriousTriggerImpl(CreatureTargetingTrigger trigger, Card referrer)
	{
		_ = genericVictoriousTriggers.TryAdd(referrer.uid, []);
		genericVictoriousTriggers[referrer.uid].Add(trigger);
	}

	public void CreatureChangeLifeImpl(Creature target, int amount, Card source)
	{
		if(amount == 0)
		{
			return;
		}
		if(amount < 0)
		{
			amount *= multiplicativeDamageModifier;
		}
		if(amount < 0 && source is Spell)
		{
			players[source.Controller].dealtSpellDamages[turn] -= amount;
		}
		RegisterLocationTemporaryLingeringEffectImpl(info: LingeringEffectInfo.Create(effect: (tg) => tg.Life += amount, referrer: target));
	}
	public void CreatureChangePowerImpl(Creature target, int amount, Card source)
	{
		if(amount == 0)
		{
			return;
		}
		RegisterLocationTemporaryLingeringEffectImpl(info: LingeringEffectInfo.Create(effect: (tg) => tg.Power += amount, referrer: target));
	}

	private void DealDamage(int player, int amount, Card source, bool alreadyDealtWithDamageModifiers = false)
	{
		if(amount < 0)
		{
			throw new Exception($"Tried to deal negative damage: {amount} by {source.Name} {source}");
		}
		if(!alreadyDealtWithDamageModifiers)
		{
			amount *= multiplicativeDamageModifier;
		}
		players[player].life -= amount;
		players[1 - player].dealtDamages[turn] += amount;
		if(source is Spell)
		{
			players[1 - player].dealtSpellDamages[turn] += amount;
		}
		RevealImpl(player, amount);
		CheckIfLost(player);
		ProcessTriggers(dealsDamageTriggers, source.uid);
	}
	private void RevealImpl(int player, int damage)
	{
		for(int i = 0; i < Math.Min(damage, players[player].deck.Size); i++)
		{
			Card c = players[player].deck.GetAt(i);
			SendShownInfos(player, new(c.ToStruct(), "Revealed"));
			players[player].deck.PushToRevealed();
			ProcessTriggers(revelationTriggers, c.uid);
			SendFieldUpdates();
		}
		players[player].deck.PopRevealedAndShuffle();
		SendShownInfos(player, null);
	}
	private void MarkNextZoneOrContinue()
	{
		if(markedZone == null)
		{
			State = GameConstantsElectricBoogaloo.State.TurnEnd;
			return;
		}
		if(turnPlayer == 0)
		{
			markedZone++;
			if(markedZone == GameConstantsElectricBoogaloo.FIELD_SIZE)
			{
				markedZone = null;
			}
		}
		else
		{
			markedZone--;
			if(markedZone < 0)
			{
				markedZone = null;
			}
		}
		initPlayer = turnPlayer;
		State = GameConstantsElectricBoogaloo.State.BattleInitGained;
	}
	private int GetMarkedZoneForPlayer(int player)
	{
		if(player == 0)
		{
			return markedZone!.Value;
		}
		else
		{
			return GameConstantsElectricBoogaloo.FIELD_SIZE - 1 - markedZone!.Value;
		}
		// Equivalent but magic:
		// return player * (GameConstants.FIELD_SIZE - 1 - 2 * markedZone!.Value) + markedZone!.Value;
	}

	private bool HandlePlayerActions()
	{
		for(int i = 0; i < players.Length; i++)
		{
			if(playerStreams[i]!.DataAvailable)
			{
				CToS_Content content = CToS_Packet.Deserialize(playerStreams[i]!).content;
				Program.replay?.packets.Add(new(i, new ReplayContent.ctos(content)));
				if(HandlePacket(content, i))
				{
					Log($"{players[i].name} is giving up, closing.");
					return true;
				}
			}
			else
			{
				Thread.Sleep(10);
			}
		}
		return false;
	}

	private bool HandlePacket(CToS_Content content, int player)
	{
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		// possible improvements: Huffman or Burrows-Wheeler+RLE
		switch(content)
		{
			case CToS_Content.surrender:
			{
				SendPacketToPlayer(new SToC_Content.game_result(new
				(
					result: GameResult.Won
				)), 1 - player);
				Log("Surrender request received");
				return true;
			}
			case CToS_Content.get_actions request:
			{
				SendPacketToPlayer(new SToC_Content.get_actions(new
				(
					location: request.value.location,
					uid: request.value.uid,
					actions: GetCardActions(player, request.value.uid, request.value.location)
				)), player);
			}
			break;
			case CToS_Content.select_option request:
			{
				bool found = false;
				foreach(CardAction action in GetCardActions(player, request.value.uid, request.value.location))
				{
					if(action.uid == request.value.action.uid)
					{
						TakeAction(player, request.value.uid, request.value.location, request.value.action.uid);
						found = true;
						break;
					}
				}
				if(!found)
				{
					Log("Tried to use an option that is not present for that card", severity: LogSeverity.Warning);
				}
				State &= ~GameConstantsElectricBoogaloo.State.InitGained;
				State |= GameConstantsElectricBoogaloo.State.ActionTaken;
			}
			break;
			case CToS_Content.pass:
			{
				switch(State)
				{
					case GameConstantsElectricBoogaloo.State.MainInitGained:
					{
						if(!players[player].passed)
						{
							if(players[1 - player].passed)
							{
								State = GameConstantsElectricBoogaloo.State.BattleStart;
							}
							else
							{
								players[player].passed = true;
								State = GameConstantsElectricBoogaloo.State.MainActionTaken;
							}
						}
					}
					break;
					case GameConstantsElectricBoogaloo.State.BattleInitGained:
					{
						if(!players[player].passed)
						{
							if(players[1 - player].passed)
							{
								State = GameConstantsElectricBoogaloo.State.DamageCalc;
							}
							else
							{
								players[player].passed = true;
								State = GameConstantsElectricBoogaloo.State.BattleActionTaken;
							}
						}
					}
					break;
					default:
						Log($"Unable to pass in state {State}", severity: LogSeverity.Warning);
						break;
				}
			}
			break;
			case CToS_Content.view_grave request:
			{
				bool opponent = request.value.for_opponent;
				SendPacketToPlayer(new SToC_Content.show_cards(new(cards: Card.ToStruct(players[opponent ? 1 - player : player].grave.GetAll()), description: $"Your {(opponent ? "opponent's" : "")} grave")), player);
			}
			break;
			default:
				throw new Exception($"ERROR: Unable to process this packet: ({content.GetType()})");
		}
		return false;
	}

	private void TakeAction(int player, uint uid, Location location, uint cardActionUid)
	{
		if(player != initPlayer)
		{
			return;
		}
		players[player].passed = false;
		if(activatedEffects.TryGetValue(uid, out List<ActivatedEffectInfo>? matchingInfos))
		{
			foreach(ActivatedEffectInfo info in matchingInfos)
			{
				if(info.CanActivate(location) && cardActionUid == info.cardActionUid)
				{
					info.effect();
					info.uses++;
					EvaluateLingeringEffects();
					SendFieldUpdates();
					return;
				}
			}
		}
		switch(location)
		{
			case Location.Hand:
			{
				Card card = players[player].hand.GetByUID(uid);
				if(cardActionUid == CastActionDescription.uid)
				{
					players[player].momentum -= card.Cost;
					CastImpl(player, card);
				}
				else
				{
					throw new NotImplementedException($"Scripted action {cardActionUid}");
				}
			}
			break;
			case Location.Quest:
			{
				if(players[player].quest.Progress >= players[player].quest.Goal)
				{
					throw new NotImplementedException($"GetActions for ignition quests");
				}
			}
			break;
			case Location.Ability:
			{
				if(players[player].abilityUsable && players[player].momentum > 0 && castTriggers.ContainsKey(players[player].ability.uid))
				{
					SendShownInfos(player, new(players[player].ability.ToStruct(), "Ability"));
					players[player].momentum--;
					players[player].abilityUsable = false;
					ProcessTriggers(castTriggers, players[player].ability.uid);
					foreach(Player p in players)
					{
						ProcessLocationBasedTargetingTriggers(triggers: genericCastTriggers, target: players[player].ability, uid: p.quest.uid);
						foreach(Card possiblyTriggeringCard in p.field.GetUsed())
						{
							ProcessLocationBasedTargetingTriggers(triggers: genericCastTriggers, target: players[player].ability, uid: possiblyTriggeringCard.uid);
						}
					}
					SendShownInfos(player, null);
				}
			}
			break;
			case Location.Field:
			{
				Creature card = players[player].field.GetByUID(uid);
				if(cardActionUid == CreatureMoveActionDescription.uid)
				{
					if(players[player].field.CanMove(card.Position, players[player].momentum))
					{
						int zone = SelectMovementZone(player, card.Position, players[player].momentum);
						players[player].momentum -= Math.Abs(card.Position - zone) * card.CalculateMovementCost();
						MoveImpl(card, zone);
					}
				}
				else
				{
					throw new NotImplementedException($"Scripted onfield option {cardActionUid}");
				}
			}
			break;
			default:
				throw new NotImplementedException($"TakeAction at {location}");
		}
	}

	public void MoveImpl(Creature card, int zone)
	{
		players[card.Controller].field.Move(card.Position, zone);
		SendFieldUpdates();
	}

	private List<CardAction> GetActivatableActions(uint uid, Location location)
	{
		List<CardAction> ret = [];
		if(activatedEffects.TryGetValue(uid, out List<ActivatedEffectInfo>? matchingInfos))
		{
			foreach(ActivatedEffectInfo info in matchingInfos)
			{
				if(info.CanActivate(location))
				{
					ret.Add(new(uid: info.cardActionUid, description: info.name));
				}
			}
		}
		return ret;
	}
	private List<CardAction> GetCardActions(int player, uint uid, Location location)
	{
		if(player != initPlayer)
		{
			return [];
		}
		EvaluateLingeringEffects();
		List<CardAction> options = GetActivatableActions(uid, location);
		switch(location)
		{
			case Location.Hand:
			{
				Card card = players[player].hand.GetByUID(uid);
				if(card.Cost <= players[player].momentum &&
					!(State.HasFlag(GameConstantsElectricBoogaloo.State.BattleStart) && card is Creature))
				{
					bool canCast = true;
					if(castTriggers.TryGetValue(card.uid, out List<Trigger>? matchingTriggers))
					{
						foreach(Trigger trigger in matchingTriggers)
						{
							EvaluateLingeringEffects();
							canCast = trigger.condition();
							if(!canCast)
							{
								break;
							}
						}
					}
					else
					{
						canCast = card is not Spell;
					}
					if(canCast)
					{
						options.Add(CastActionDescription);
					}
				}
			}
			break;
			case Location.Ability:
			{
				if(players[player].abilityUsable && players[player].momentum > 0 && castTriggers.TryGetValue(players[player].ability.uid, out List<Trigger>? matchingTriggers))
				{
					bool canActivate = true;
					foreach(Trigger trigger in matchingTriggers)
					{
						canActivate = trigger.condition();
						if(!canActivate)
						{
							break;
						}
					}
					if(canActivate)
					{
						options.Add(AbilityUseActionDescription);
					}
				}
			}
			break;
			case Location.Field:
			{
				Creature card = players[player].field.GetByUID(uid);
				if(players[player].field.CanMove(card.Position, players[player].momentum))
				{
					options.Add(CreatureMoveActionDescription);
				}
			}
			break;
			case Location.Quest:
				Log("Quests are not foreseen to have activated abilities", severity: LogSeverity.Warning);
				break;
			default:
				throw new NotImplementedException($"GetCardActions at {location}");
		}
		return options;
	}

	public bool AskYesNoImpl(int player, string question)
	{
		Log("Asking yes no");
		SendPacketToPlayer(new SToC_Content.yes_no(new(question)), player);
		Log("Receiving");
		return ReceivePacketFromPlayer<CToS_Content.yes_no>(player).value.yes;
	}
	private void SendFieldUpdates()
	{
		EvaluateLingeringEffects();
		for(int i = 0; i < players.Length; i++)
		{
			SendFieldUpdate(i);
		}
	}
	private void SendFieldUpdate(int player)
	{
		SendPacketToPlayer(new SToC_Content.field_update(new
		(
			turn: (uint)turn + 1,
			has_initiative: State != GameConstantsElectricBoogaloo.State.UNINITIALIZED && initPlayer == player,
			is_battle_direction_left_to_right: player == turnPlayer,
			marked_zone: player == 0 ? markedZone : (GameConstantsElectricBoogaloo.FIELD_SIZE - 1 - markedZone),
			own_field: new
			(
				ability: players[player].ability.ToStruct(),
				quest: players[player].quest.ToStruct(),
				deck_size: (uint)players[player].deck.Size,
				grave_size: (uint)players[player].grave.Size,
				life: players[player].life,
				name: players[player].name,
				momentum: players[player].momentum,
				field: [.. players[player].field.ToStruct()],
				hand: [.. players[player].hand.ToStruct()]
			),
			opp_field: new
			(
				ability: players[1 - player].ability.ToStruct(),
				quest: players[1 - player].quest.ToStruct(),
				deck_size: (uint)players[1 - player].deck.Size,
				grave_size: (uint)players[1 - player].grave.Size,
				life: players[1 - player].life,
				name: players[1 - player].name,
				momentum: players[1 - player].momentum,
				field: [.. players[1 - player].field.ToStruct()],
				hand: [.. players[1 - player].hand.ToHiddenStruct()]
			)
		)), player);
	}
	public static Card[] SelectCardsCustom(int player, string description, Card[] cards, Func<Card[], bool> isValidSelection)
	{
		Log("Select cards custom");
		SendPacketToPlayer(new SToC_Content.select_cards_custom(new
		(
			cards: Card.ToStruct(cards),
			description: description,
			initial_state: isValidSelection([])
		)), player);

		Log("request sent");
		while(true)
		{
			CToS_Content packet = CToS_Packet.Deserialize(playerStreams[player]!).content;
			Log("request received");
			Program.replay?.packets.Add(new(player, new ReplayContent.ctos(packet)));
			if(packet is CToS_Content.select_cards_custom response)
			{
				Log("final response");
				Card[] ret = UidsToCards(cards, response.value.uids);
				if(!isValidSelection(ret))
				{
					throw new Exception("Player somethow selected invalid cards");
				}
				Log("returning");
				return ret;
			}
			if(packet is not CToS_Content.select_cards_custom_intermediate)
			{
				Log($"Ignoring packet of type {packet.GetType()} during SelectCustom");
				continue;
			}
			Log("Serialized packet");
			SendPacketToPlayer(new SToC_Content.select_cards_custom_intermediate(new
			(
				is_valid: isValidSelection([.. ((CToS_Content.select_cards_custom_intermediate)packet).value.uids.ConvertAll(x => Array.Find(cards, y => y.uid == x)!)])
			)), player);
			Log("sent packet");
		}

	}

	public static Card[] UidsToCards(Card[] cards, List<uint> uids)
	{
		Card[] ret = new Card[uids.Count];
		for(int i = 0; i < ret.Length; i++)
		{
			bool found = false;
			for(int j = 0; j < cards.Length; j++)
			{
				if(cards[j].uid == uids[i])
				{
					ret[i] = cards[j];
					found = true;
					break;
				}
			}
			if(!found)
			{
				throw new Exception($"Selected uid {uids[i]} could not be found in the source array");
			}
		}
		return ret;
	}
	private int SelectMovementZone(int player, int position, int momentum)
	{
		SendPacketToPlayer(new SToC_Content.select_zone(new
		(
			options: [.. players[player].field.GetMovementOptions(position, momentum)]
		)), player);
		return ReceivePacketFromPlayer<CToS_Content.select_zone>(player).value.zone;
	}
	private int GetCastCountImpl(int player, string name)
	{
		return players[player].castCounts.GetValueOrDefault(name, 0);
	}
	private int GetTurnImpl()
	{
		return turn;
	}
	private int GetPlayerLifeImpl(int player)
	{
		return players[player].life;
	}
	private void DrawImpl(int player, int amount)
	{
		players[player].Draw(amount);
		SendFieldUpdates();
	}
	private void MoveToFieldImpl(int choosingPlayer, int targetPlayer, Creature creature, Card? source)
	{
		EvaluateLingeringEffects();
		bool wasAlreadyOnField = creature.Location == Location.Field;
		_ = RemoveCardFromItsLocation(creature);
		int zone = SelectZoneImpl(choosingPlayer: choosingPlayer, targetPlayer: targetPlayer);
		if(creature.Controller != targetPlayer)
		{
			RegisterControllerChange(creature);
		}
		players[targetPlayer].field.Add(creature, zone);
		RemoveOutdatedTemporaryLingeringEffects(creature);
		if(!wasAlreadyOnField)
		{
			foreach(Player p in players)
			{
				ProcessLocationBasedTargetingTriggers(genericEnterFieldTriggers, target: creature, uid: p.quest.uid);
				foreach(Creature possiblyTriggeringCard in p.field.GetUsed())
				{
					ProcessLocationBasedTargetingTriggers(genericEnterFieldTriggers, target: creature, uid: possiblyTriggeringCard.uid);
				}
				if(creature.Keywords.ContainsKey(Keyword.Token))
				{
					if(source == null)
					{
						throw new Exception($"Moving token {creature.Name} to field but source was null");
					}
					ProcessTokenCreationTriggers(tokenCreationTriggers, token: creature, source: source, uid: p.quest.uid);
					foreach(Creature possiblyTriggeringCard in p.field.GetUsed())
					{
						ProcessTokenCreationTriggers(tokenCreationTriggers, token: creature, source: source, uid: possiblyTriggeringCard.uid);
					}
				}
			}
		}
	}
	private void CastImpl(int player, Card card)
	{
		EvaluateLingeringEffects();
		bool isNew = !card.isInitialized;
		if(isNew)
		{
			card.Init();
			card.isInitialized = true;
		}
		_ = RemoveCardFromItsLocation(card);
		SendShownInfos(player, new ShownInfo(card.ToStruct(), CastActionDescription.description));
		if(!isNew)
		{
			switch(card)
			{
				case Creature creature:
				{
					MoveToFieldImpl(player, player, creature, null);
				}
				break;
				case Spell:
				{
					AddCardToLocation(card, Location.Grave);
				}
				break;
				default:
					throw new NotImplementedException($"Casting {card.GetType()} cards");
			}
		}
		if(!players[player].castCounts.TryGetValue(card.Name, out int value))
		{
			value = 0;
			players[player].castCounts[card.Name] = value;
		}
		players[player].castCounts[card.Name] = ++value;
		ProcessTriggers(castTriggers, card.uid);
		foreach(Player p in players)
		{
			ProcessLocationBasedTargetingTriggers(genericCastTriggers, target: card, uid: p.quest.uid);
			foreach(Card possiblyTriggeringCard in p.field.GetUsed())
			{
				ProcessLocationBasedTargetingTriggers(genericCastTriggers, target: card, uid: possiblyTriggeringCard.uid);
			}
		}
		SendFieldUpdates();
		SendShownInfos(player, null);
	}

	public void SendShownInfos(int forPlayer, ShownInfo? info)
	{
		for(int i = 0; i < players.Length; i++)
		{
			SendPacketToPlayer(new SToC_Content.show_info(new(player: forPlayer, info)), i);
		}
	}

	public void ResetAbilityImpl(int player)
	{
		players[player].abilityUsable = true;
	}

	public void RegisterCastTriggerImpl(Trigger trigger, Card referrer)
	{
		_ = castTriggers.TryAdd(referrer.uid, []);
		castTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterDealsDamageTriggerImpl(Trigger trigger, Card referrer)
	{
		_ = dealsDamageTriggers.TryAdd(referrer.uid, []);
		dealsDamageTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterGenericCastTriggerImpl(LocationBasedTargetingTrigger trigger, Card referrer)
	{
		_ = genericCastTriggers.TryAdd(referrer.uid, []);
		genericCastTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterRevelationTriggerImpl(Trigger trigger, Card referrer)
	{
		_ = revelationTriggers.TryAdd(referrer.uid, []);
		revelationTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterGenericEntersFieldTriggerImpl(LocationBasedTargetingTrigger trigger, Card referrer)
	{
		_ = genericEnterFieldTriggers.TryAdd(referrer.uid, []);
		genericEnterFieldTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterYouDiscardTriggerImpl(LocationBasedTrigger trigger, Card referrer)
	{
		_ = youDiscardTriggers.TryAdd(referrer.uid, []);
		youDiscardTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterDiscardTriggerImpl(Trigger trigger, Card referrer)
	{
		_ = discardTriggers.TryAdd(referrer.uid, []);
		discardTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterStateReachedTriggerImpl(StateReachedTrigger trigger, Card referrer)
	{
		if(trigger.influenceLocation == Location.Any)
		{
			alwaysActiveStateReachedTriggers.Add(trigger);
		}
		else
		{
			_ = stateReachedTriggers.TryAdd(referrer.uid, []);
			stateReachedTriggers[referrer.uid].Add(trigger);
		}
	}
	public void RegisterLingeringEffectImpl(LingeringEffectInfo info)
	{
		if(info.influenceLocation == Location.Any)
		{
			alwaysActiveLingeringEffects.Add(info);
		}
		else
		{
			_ = lingeringEffects.TryAdd(info.referrer.uid, new(this));
			lingeringEffects[info.referrer.uid].Add(info);
		}
	}
	public void RegisterLocationTemporaryLingeringEffectImpl(LingeringEffectInfo info)
	{
		_ = locationTemporaryLingeringEffects.TryAdd(info.referrer.uid, new(this));
		locationTemporaryLingeringEffects[info.referrer.uid].Add(info);
	}

	private void RegisterStateTemporaryLingeringEffectImpl(LingeringEffectInfo info, GameConstantsElectricBoogaloo.State state)
	{
		_ = stateTemporaryLingeringEffects.TryAdd(state, new(this));
		stateTemporaryLingeringEffects[state].Add(info);
	}
	public void RegisterActivatedEffectImpl(ActivatedEffectInfo info)
	{
		if(info.name == AbilityUseActionDescription.description ||
			info.name == CastActionDescription.description ||
			info.name == CreatureMoveActionDescription.description)
		{
			throw new Exception($"Activated Effects should not be named {info.name}, this is a reserved name.");
		}
		_ = activatedEffects.TryAdd(info.referrer.uid, []);
		activatedEffects[info.referrer.uid].Add(info);
	}
	public void RegisterVictoriousTriggerImpl(Trigger trigger, Card referrer)
	{
		_ = victoriousTriggers.TryAdd(referrer.uid, []);
		victoriousTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterAttackTriggerImpl(CreatureTargetingTrigger trigger, Card referrer)
	{
		_ = attackTriggers.TryAdd(referrer.uid, []);
		attackTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterDeathTriggerImpl(CreatureTargetingTrigger trigger, Card referrer)
	{
		_ = deathTriggers.TryAdd(referrer.uid, []);
		deathTriggers[referrer.uid].Add(trigger);
	}
	public void RegisterGenericDeathTriggerImpl(CreatureTargetingTrigger trigger, Card referrer)
	{
		_ = genericDeathTriggers.TryAdd(referrer.uid, []);
		genericDeathTriggers[referrer.uid].Add(trigger);
	}
	private void RegisterTokenCreationTriggerImpl(TokenCreationTrigger trigger, Card referrer)
	{
		_ = tokenCreationTriggers.TryAdd(referrer.uid, []);
		tokenCreationTriggers[referrer.uid].Add(trigger);
	}
	public Creature?[] GetFieldImpl(int player)
	{
		return players[player].field.GetAll();
	}
	public Creature[] GetFieldUsedImpl(int player)
	{
		return players[player].field.GetUsed();
	}
	public Card[] GetGraveImpl(int player)
	{
		return players[player].grave.GetAll();
	}
	public Card[] GetHandImpl(int player)
	{
		return players[player].hand.GetAll();
	}
	public void PlayerChangeLifeImpl(int player, int amount, Card source)
	{
		if(amount > 0)
		{
			players[player].life += amount;
		}
		else
		{
			DealDamage(player: player, amount: -amount, source: source);
		}
	}
	public Card GatherImpl(int player, int amount)
	{
		Card[] possibleCards = players[player].deck.GetRange(0, amount);
		Card target = CardUtils.SelectSingleCard(player: player, cards: possibleCards, description: "Select card to gather");
		MoveToHandImpl(player, target);
		players[player].deck.Shuffle();
		return target;
	}
	public void PayLifeImpl(int player, int amount)
	{
		players[player].life -= amount;
		CheckIfLost(player);
	}
	public int GetIgniteDamageImpl(int player)
	{
		EvaluateLingeringEffects();
		return players[player].igniteDamage;
	}
	public void ChangeIgniteDamageImpl(int player, int amount)
	{
		players[player].baseIgniteDamage += amount;
	}
	public void ChangeIgniteDamageTemporaryImpl(int player, int amount)
	{
		players[player].igniteDamage += amount;
	}
	public void PlayerChangeMomentumImpl(int player, int amount)
	{
		players[player].momentum += amount;
		if(players[player].momentum < 0)
		{
			players[player].momentum = 0;
		}
	}
	public void MoveToHandImpl(int player, Card card)
	{
		EvaluateLingeringEffects();
		switch(card.Location)
		{
			case Location.Deck:
				players[card.Controller].deck.Remove(card);
				break;
			case Location.Hand:
			{
				if(card.Controller == player)
				{
					Log($"Tried to add {card.Name} from the hand to the same hand", severity: LogSeverity.Warning);
				}
				else
				{
					players[card.Controller].hand.Remove(card);
				}
			}
			break;
			case Location.Field:
				players[card.Controller].field.Remove((Creature)card);
				break;
			case Location.Grave:
				players[card.Controller].grave.Remove(card);
				break;
			default:
				throw new Exception($"Cannot add a card from {card.Location} to hand");
		}
		if(!(card is Creature creature && creature.Keywords.ContainsKey(Keyword.Token)))
		{
			if(card.Controller != player)
			{
				RegisterControllerChange(card);
			}
			players[player].hand.Add(card);
		}
		else
		{
			Log($"Tried to add a token to hand", severity: LogSeverity.Warning);
		}
		RemoveOutdatedTemporaryLingeringEffects(card);
	}

	private void SetDamageMultiplierImpl(int value)
	{
		multiplicativeDamageModifier = value;
	}

	private int GetDamageMultiplierImpl()
	{
		return multiplicativeDamageModifier;
	}

	public Card[] GetDiscardableImpl(int player, Card? ignore)
	{
		return players[player].hand.GetDiscardable(ignore);
	}
	private void RegisterControllerChange(Card card)
	{
		RegisterLocationTemporaryLingeringEffectImpl(info: LingeringEffectInfo.Create(effect: (target) => target.Controller = 1 - target.Controller, referrer: card, influenceLocation: Location.Field));
	}
	public void DestroyImpl(Creature card)
	{
		switch(card.Location)
		{
			case Location.Field:
			{
				players[card.Controller].field.Remove(card);
				AddCardToLocation(card, Location.Grave);
			}
			break;
			case Location.UNKNOWN:
			{
				Log($"Destroying {card.Name} at UNKNOWN", severity: LogSeverity.Warning);
			}
			break;
			default:
				throw new Exception($"Destroying {card.Name} at {card.Location} is not supported");
		}
		if(card.Keywords.ContainsKey(Keyword.Brittle))
		{
			players[card.Controller].brittleDeathCounts[turn]++;
		}
		players[card.Controller].deathCounts[turn]++;
		ProcessCreatureTargetingTriggers(deathTriggers, target: card, uid: card.uid, location: card.Location);
		SendFieldUpdates();
		foreach(Player player in players)
		{
			foreach(Card fieldCard in player.field.GetUsed())
			{
				ProcessCreatureTargetingTriggers(genericDeathTriggers, target: card, uid: fieldCard.uid, location: Location.Field);
			}
			foreach(Card graveCard in player.grave.GetAll())
			{
				ProcessCreatureTargetingTriggers(genericDeathTriggers, target: card, uid: graveCard.uid, location: Location.Grave);
			}
			foreach(Card handCard in player.hand.GetAll())
			{
				ProcessCreatureTargetingTriggers(genericDeathTriggers, target: card, uid: handCard.uid, location: Location.Hand);
			}
		}
		foreach(Player player in players)
		{
			ProcessCreatureTargetingTriggers(triggers: genericDeathTriggers, target: card, location: Location.Quest, uid: player.quest.uid);
		}
	}
	private void RemoveOutdatedTemporaryLingeringEffects(Card card)
	{
		locationTemporaryLingeringEffects.GetValueOrDefault(card.uid)?.RemoveAll(info => !IsInLocation(info.influenceLocation, card.Location));
	}
	public bool RemoveCardFromItsLocation(Card card)
	{
		switch(card.Location)
		{
			case Location.Hand:
				players[card.Controller].hand.Remove(card);
				break;
			case Location.Field:
				players[card.Controller].field.Remove((Creature)card);
				break;
			case Location.Grave:
				players[card.Controller].grave.Remove(card);
				break;
			case Location.Deck:
				players[card.Controller].deck.Remove(card);
				break;
			default:
				return false;
		}
		return true;
	}
	public void ReturnCardsToDeckImpl(Card[] cards)
	{
		EvaluateLingeringEffects();
		bool[] shouldShuffle = new bool[players.Length];
		foreach(Card card in cards)
		{
			if(RemoveCardFromItsLocation(card))
			{
				shouldShuffle[card.BaseController] = true;
				AddCardToLocation(card, Location.Deck);
			}
			else
			{
				Log($"Could not move {card} from {card.Location} to deck.");
			}
		}
		for(int i = 0; i < shouldShuffle.Length; i++)
		{
			if(shouldShuffle[i])
			{
				players[i].deck.Shuffle();
			}
		}
	}
	public Card[] SelectCardsImpl(int player, Card[] cards, uint amount, string description)
	{
		if(cards.Length < amount)
		{
			throw new Exception($"Tried to let a player select from a too small collection ({cards.Length} < {amount})");
		}
		SendPacketToPlayer(new SToC_Content.select_cards(new
		(
			amount: amount,
			cards: Card.ToStruct(cards),
			description: description
		)), player);
		List<uint> uids = ReceivePacketFromPlayer<CToS_Content.select_cards>(player).value.uids;
		if(uids.Count != amount)
		{
			throw new Exception($"Selected the wrong amount of cards ({uids.Count} != {amount})");
		}
		return UidsToCards(cards, uids);
	}
	public void DiscardAmountImpl(int player, uint amount)
	{
		Card[] cards = players[player].hand.GetDiscardable(null);
		amount = Math.Min((uint)cards.Length, amount);
		if(amount == 0)
		{
			return;
		}
		Card[] targets = SelectCardsImpl(player: player, amount: amount, cards: cards, description: "Select cards to discard");
		foreach(Card target in targets)
		{
			DiscardImpl(target);
		}
	}
	private void AddCardToLocation(Card card, Location location)
	{
		if(card.BaseController < 0)
		{
			throw new Exception($"Tried to add a '{card.Name}' to {location} but BaseController was negative");
		}
		switch(location)
		{
			case Location.Deck:
			{
				players[card.BaseController].deck.Add(card);
			}
			break;
			case Location.Grave:
			{
				players[card.BaseController].grave.Add(card);
			}
			break;
			default:
			{
				throw new Exception($"Tried to add card {card.Name} to location {location} of {card.BaseController}");
			}
		}
		RemoveOutdatedTemporaryLingeringEffects(card);
	}
	public void DiscardImpl(Card card)
	{
		EvaluateLingeringEffects();
		if(card.Location != Location.Hand || !card.CanBeDiscarded())
		{
			throw new Exception($"Tried to discard a card that is not in the hand but at {card.Location}");
		}
		players[card.Controller].hand.Remove(card);
		AddCardToLocation(card, Location.Grave);
		players[card.Controller].discardCounts[turn]++;
		Player player = players[card.Controller];
		ProcessTriggers(discardTriggers, uid: card.uid);
		ProcessLocationBasedTriggers(youDiscardTriggers, Location.Quest, player.quest.uid);
		foreach(Card c in player.hand.GetAll())
		{
			ProcessLocationBasedTriggers(youDiscardTriggers, Location.Hand, uid: c.uid);
		}
		foreach(Creature c in player.field.GetUsed())
		{
			ProcessLocationBasedTriggers(youDiscardTriggers, Location.Field, uid: c.uid);
		}
	}
	public void CreateTokenOnFieldImpl(int player, int power, int life, string name, Card source)
	{
		MoveToFieldImpl(player, player, CreateTokenImpl(player, power, life, name), source);
	}
	public Token CreateTokenImpl(int player, int power, int life, string name)
	{
		if(!players[player].field.HasEmpty())
		{
			throw new Exception($"Tried to create a token but the field is full");
		}
		Token token = new
		(
			Name: name,
			Text: "",
			OriginalCost: 0,
			OriginalLife: life,
			OriginalPower: power,
			OriginalController: player
		);
		return token;
	}
	public void CreateTokenCopyOnFieldImpl(int player, Creature card, Card source)
	{
		MoveToFieldImpl(player, player, CreateTokenCopyImpl(player, card), source);
	}
	public Creature CreateTokenCopyImpl(int player, Creature card)
	{
		if(!players[player].field.HasEmpty())
		{
			throw new Exception($"Tried to create a token but the field is full");
		}
		Creature token;
		if(card.GetType() == typeof(Token))
		{
			token = CreateTokenImpl(player: player, power: card.Power, life: card.Life, name: card.Name);
			foreach(Keyword keyword in card.Keywords.Keys)
			{
				token.RegisterKeyword(keyword, card.Keywords[keyword]);
			}
		}
		else
		{
			token = (Creature)CreateBasicCard(card.GetType(), player);
			token.RegisterKeyword(Keyword.Token);
		}
		return token;
	}

	public void RemoveLingeringEffectImpl(LingeringEffectInfo info)
	{
		if(info.influenceLocation == Location.Any)
		{
			alwaysActiveLingeringEffects.Remove(info);
		}
		else
		{
			if(!lingeringEffects.ContainsKey(info.referrer.uid))
			{
				Log("Tried to remove Lingering Effect of a card that has no registered Lingering Effects", severity: LogSeverity.Error);
				return;
			}
			lingeringEffects[info.referrer.uid].Remove(info);
		}
	}

	public int SelectZoneImpl(int choosingPlayer, int targetPlayer)
	{
		bool[] options = players[targetPlayer].field.GetPlacementOptions();
		if(choosingPlayer != targetPlayer)
		{
			Array.Reverse(options);
		}
		SendPacketToPlayer(new SToC_Content.select_zone(new([.. options])), choosingPlayer);
		int zone = ReceivePacketFromPlayer<CToS_Content.select_zone>(choosingPlayer).value.zone;
		if(choosingPlayer != targetPlayer)
		{
			zone = GameConstantsElectricBoogaloo.FIELD_SIZE - zone - 1;
		}
		return zone;
	}
	public int GetDiscardCountXTurnsAgoImpl(int player, int turns)
	{
		if(turn < turns || players[player].discardCounts.Count <= turn - turns)
		{
			Log($"Attempted to get discard count before the game began ({turn - turns}) for player {players[player].name}", severity: LogSeverity.Warning);
			return 0;
		}
		return players[player].discardCounts[turn - turns];
	}

	public int GetDamageDealtXTurnsAgoImpl(int player, int turns)
	{
		if(turn < turns || players[player].dealtDamages.Count <= turn - turns)
		{
			Log($"Attempted to get damage dealt before the game began ({turn - turns}) for player {players[player].name}", severity: LogSeverity.Warning);
			return 0;
		}
		return players[player].dealtDamages[turn - turns];
	}
	public int GetSpellDamageDealtXTurnsAgoImpl(int player, int turns)
	{
		if(turn < turns || players[player].dealtSpellDamages.Count <= turn - turns)
		{
			Log($"Attempted to get spell damage dealt before the game began ({turn - turns}) for player {players[player].name}", severity: LogSeverity.Warning);
			return 0;
		}
		return players[player].dealtSpellDamages[turn - turns];
	}
	public int GetBrittleDeathCountXTurnsAgoImpl(int player, int turns)
	{
		if(turn < turns || players[player].brittleDeathCounts.Count <= turn - turns)
		{
			Log($"Attempted to get brittle death count before the game began ({turn - turns}) for player {players[player].name}", severity: LogSeverity.Warning);
			return 0;
		}
		return players[player].brittleDeathCounts[turn - turns];
	}
	public int GetDeathCountXTurnsAgoImpl(int player, int turns)
	{
		if(turn < turns || players[player].deathCounts.Count <= turn - turns)
		{
			Log($"Attempted to get death count before the game began ({turn - turns}) for player {players[player].name}", severity: LogSeverity.Warning);
			return 0;
		}
		return players[player].deathCounts[turn - turns];
	}

	public static T ReceivePacketFromPlayer<T>(int player) where T : CToS_Content
	{
		CToS_Content packet = CToS_Packet.Deserialize(playerStreams[player]!).content;
		Program.replay?.packets.Add(new(player, new ReplayContent.ctos(packet)));
		return (T)packet;
	}
	public static void SendPacketToPlayer(SToC_Content content, int player)
	{
		byte[] payload = new SToC_Packet(content).Serialize();
		Program.replay?.packets.Add(new(player, new ReplayContent.stoc(content)));
		playerStreams[player]!.Write(payload);
	}
}
