using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CardGameUtils.GameEnumsAndStructs;
using CardGameUtils.Base;

namespace CardGameCore;

internal abstract class Card
{
	public bool isInitialized;
	public string Name, Text;
	public PlayerClass CardClass;
	public uint uid;
	private int _cost = -1;
	private int _baseController = -1;
	public int Cost
	{
		get => _cost;
		set
		{
			_cost = value;
			if(_cost < 0)
			{
				_cost = 0;
			}
		}
	}
	public readonly int BaseCost;
	public Location Location;
	public int Controller { get; set; }
	public int BaseController
	{
		get => _baseController;
		set
		{
			if(_baseController == -1)
			{
				_baseController = value;
			}
		}
	}
	public abstract void Init();

	public Card(PlayerClass CardClass,
		string Name,
		string Text,
		int OriginalCost = 0,
		Location OriginalLocation = Location.UNKNOWN)
	{
		this.CardClass = CardClass;
		this.Name = Name;
		this.Text = Text;
		this.BaseCost = OriginalCost;
		this.Location = OriginalLocation;
		this.uid = DuelCore.UIDCount;
		DuelCore.UIDCount++;
		ResetToBaseState();
	}
	#region ScriptingFunctions

	internal static RemoveLingeringEffectDelegate RemoveLingeringEffect = (_) => { };
	internal static GetDamageMultiplierDelegate GetDamageMultiplier = () => 1;
	internal static SetDamageMultiplierDelegate SetDamageMultiplier = (_) => { };
	internal static RegisterTriggerDelegate RegisterCastTrigger = (_, _) => { };
	internal static RegisterLocationBasedTargetingTriggerDelegate RegisterGenericCastTrigger = (_, _) => { };
	internal static RegisterTokenCreationTriggerDelegate RegisterTokenCreationTrigger = (_, _) => { };
	internal static RegisterLocationBasedTargetingTriggerDelegate RegisterGenericEntersFieldTrigger = (_, _) => { };
	internal static RegisterTriggerDelegate RegisterRevelationTrigger = (_, _) => { };
	internal static RegisterLocationBasedTriggerDelegate RegisterYouDiscardTrigger = (_, _) => { };
	internal static RegisterTriggerDelegate RegisterDiscardTrigger = (_, _) => { };
	internal static RegisterStateReachedTriggerDelegate RegisterStateReachedTrigger = (_, _) => { };
	internal static RegisterTriggerDelegate RegisterVictoriousTrigger = (_, _) => { };
	internal static RegisterCreatureTargetingTriggerDelegate RegisterAttackTrigger = (_, _) => { };
	internal static RegisterCreatureTargetingTriggerDelegate RegisterDeathTrigger = (_, _) => { };
	internal static RegisterCreatureTargetingTriggerDelegate RegisterGenericDeathTrigger = (_, _) => { };
	internal static RegisterCreatureTargetingTriggerDelegate RegisterGenericVictoriousTrigger = (_, _) => { };
	internal static RegisterTriggerDelegate RegisterDealsDamageTrigger = (_, _) => { };
	internal static RegisterLingeringEffectDelegate RegisterLingeringEffect = (_) => { };
	internal static RegisterLingeringEffectDelegate RegisterLocationTemporaryLingeringEffect = (_) => { };
	internal static RegisterStateTemporaryLingeringEffectDelegate RegisterStateTemporaryLingeringEffect = (_, _) => { };
	internal static RegisterActivatedEffectDelegate RegisterActivatedEffect = (_) => { };
	internal static GetCardsInLocationDelegate GetGrave = (_) => [];
	internal static GetWholeFieldDelegate GetField = (_) => [];
	internal static GetFieldUsedDelegate GetFieldUsed = (_) => [];
	internal static GetCardsInLocationDelegate GetHand = (_) => [];
	internal static SelectCardsDelegate SelectCards = (_, _, _, _) => [];
	internal static DiscardDelegate Discard = (_) => { };
	internal static DiscardAmountDelegate DiscardAmount = (_, _) => { };
	internal static CreateTokenDelegate CreateToken = (_, _, _, _) => new ClientCoreDummyToken();
	internal static CreateTokenOnFieldDelegate CreateTokenOnField = (_, _, _, _, _) => { };
	internal static CreateTokenCopyDelegate CreateTokenCopy = (_, _) => new ClientCoreDummyToken();
	internal static CreateTokenCopyOnFieldDelegate CreateTokenCopyOnField = (_, _, _) => { };
	internal static GetYXTurnsAgoDelegate GetDiscardCountXTurnsAgo = (_, _) => -1;
	internal static GetYXTurnsAgoDelegate GetDamageDealtXTurnsAgo = (_, _) => -1;
	internal static GetYXTurnsAgoDelegate GetSpellDamageDealtXTurnsAgo = (_, _) => -1;
	internal static GetYXTurnsAgoDelegate GetBrittleDeathCountXTurnsAgo = (_, _) => -1;
	internal static GetYXTurnsAgoDelegate GetDeathCountXTurnsAgo = (_, _) => -1;
	internal static PlayerChangeLifeDelegate PlayerChangeLife = (_, _, _) => { };
	internal static PlayerChangeMomentumDelegate PlayerChangeMomentum = (_, _) => { };
	internal static CastDelegate Cast = (_, _) => { };
	internal static DrawDelegate Draw = (_, _) => { };
	internal static DestroyDelegate Destroy = (_) => { };
	internal static AskYesNoDelegate AskYesNo = (_, _) => false;
	internal static GetIgniteDamageDelegate GetIgniteDamage = (_) => -1;
	internal static ChangeIgniteDamageDelegate ChangeIgniteDamage = (_, _) => { };
	internal static ChangeIgniteDamageDelegate ChangeIgniteDamageTemporary = (_, _) => { };
	internal static GetTurnDelegate GetTurn = () => -1;
	internal static GetPlayerLifeDelegate GetPlayerLife = (_) => -1;
	internal static PayLifeDelegate PayLife = (_, _) => { };
	internal static GatherDelegate Gather = (_, _) => new ClientCoreDummyCard();
	internal static MoveDelegate Move = (_, _) => { };
	internal static SelectZoneDelegate SelectZone = (_, _) => -1;
	internal static MoveToHandDelegate MoveToHand = (_, _) => { };
	internal static MoveToFieldDelegate MoveToField = (_, _, _, _) => { };
	internal static GetCastCountDelegate GetCastCount = (_, _) => -1;
	internal static ReturnCardsToDeckDelegate ReturnCardsToDeck = (_) => { };
	internal static RevealDelegate Reveal = (_, _) => { };
	internal static GetDiscardableDelegate GetDiscardable = (_, _) => [];
	internal static RefreshAbilityDelegate RefreshAbility = (_) => { };
	internal static CreatureChangeStatDelegate CreatureChangeLife = (_, _, _) => { };
	internal static CreatureChangeStatDelegate CreatureChangePower = (_, _, _) => { };
	#endregion ScriptingFunctions
	public virtual void ResetToBaseState()
	{
		_cost = BaseCost;
		Controller = BaseController;
	}

	public virtual bool CanBeDiscarded() => true;

	public abstract CardStruct ToStruct(bool client = false);

	public static bool operator ==(Card? first, Card? second)
	{
		if(first is null)
		{
			return true;
		}

		if(second is null)
		{
			return false;
		}

		return first.uid == second.uid;
	}

	public static bool operator !=(Card? first, Card? second)
	{
		return !(first == second);
	}

	public override bool Equals(object? obj)
	{
		if(ReferenceEquals(this, obj))
		{
			return true;
		}

		if(obj is null)
		{
			return false;
		}

		return obj.GetType() == GetType() && (Card)obj == this;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	internal static List<CardStruct> ToStruct(Card[] cards)
	{
		return [.. Array.ConvertAll(cards, x => x.ToStruct())];
	}
}
internal class ClientCoreDummyCard : Card
{
	public ClientCoreDummyCard() : base(PlayerClass.UNKNOWN, "UNINITIALIZED", "UNINITIALIZED")
	{ }
	public override void Init()
	{
	}

	public override CardStruct ToStruct(bool client = false)
	{
		return new CardStruct("DUMMY", "DUMMY", PlayerClass.UNKNOWN, Location.UNKNOWN, uint.MaxValue, -1, -1, new TypeSpecifics.unknown());
	}
}
internal class ClientCoreDummyToken : Token
{
	public ClientCoreDummyToken() : base("UNINITIALIZED", "UNINITIALIZED", -1, -1, -1, -1)
	{ }
	public override void Init()
	{
	}
}

internal abstract partial class Creature : Card
{
	public readonly int BaseLife, BasePower;
	public int damageCap = -1, baseDamageCap = -1;

	private int _life, _power;
	public int Position;
	public int Life
	{
		get => _life;
		set
		{
			if(damageCap > -1 && _life - value > damageCap)
			{
				_life -= damageCap;
			}
			else
			{
				_life = value;
			}
			if(_life < 0)
			{
				_life = 0;
			}
		}
	}
	public int Power
	{
		get => _power;
		set
		{
			_power = value;
			if(_power < 0)
			{
				_power = 0;
			}
		}
	}
	public Dictionary<Keyword, int> Keywords = [];
	public Creature(PlayerClass CardClass,
		string Name,
		string Text,
		int OriginalCost,
		int OriginalLife,
		int OriginalPower)
	: base(CardClass: CardClass,
		Name: Name,
		Text: Text,
		OriginalCost: OriginalCost)
	{
		BaseLife = OriginalLife;
		BasePower = OriginalPower;
		ResetToBaseState();
	}
	public int CalculateMovementCost()
	{
		return 1 + Keywords.GetValueOrDefault(Keyword.Colossal, 0);
	}
	public void RegisterKeyword(Keyword keyword, int amount = 0)
	{
		Keywords[keyword] = amount;
	}

	public override void ResetToBaseState()
	{
		base.ResetToBaseState();
		_life = BaseLife;
		_power = BasePower;
		damageCap = baseDamageCap;
	}

	public override CardStruct ToStruct(bool client = false)
	{
		StringBuilder text = new();
		if(client)
		{
			_ = text.Append(Text);
		}
		else
		{
			if(Keywords.Count > 0)
			{
				foreach(KeyValuePair<Keyword, int> keyword in Keywords)
				{
					_ = text.Append('[').Append(keyword.Key).Append("] ");
					if(keyword.Value != 0)
					{
						if(keyword.Key == Keyword.Colossal)
						{
							_ = text.Append('+');
						}
						_ = text.Append(keyword.Value);
					}
					_ = text.Append('\n');
				}
			}
			_ = text.Append(KeywordRegex().Replace(Text, ""));
		}
		return new CardStruct(name: Name,
			text: text.ToString(),
			card_class: CardClass,
			uid: uid, location: Location,
			controller: Controller,
			base_controller: BaseController,
			type_specifics: new TypeSpecifics.creature(new(
				base_cost: BaseCost, cost: Cost, base_life: BaseLife, life: Life,
				base_power: BasePower, power: Power, position: Position, damage_cap: damageCap)));
	}

	[GeneratedRegex(@"(?m:^\[.+\](?: \+?\d+)?$)\n?")]
	private static partial Regex KeywordRegex();
}

internal abstract class Spell(PlayerClass CardClass,
	string Name,
	string Text,
	int OriginalCost = 0,
	Location OriginalLocation = Location.UNKNOWN,
	bool IsClassAbility = false,
	bool CanBeClassAbility = false) : Card(CardClass: CardClass,
		Name: Name,
		Text: Text,
		OriginalCost: OriginalCost,
		OriginalLocation: OriginalLocation)
{
	public bool IsClassAbility = IsClassAbility, CanBeClassAbility = CanBeClassAbility;

	public override CardStruct ToStruct(bool client = false)
	{
		return new CardStruct(name: Name,
			text: Text,
			card_class: CardClass,
			uid: uid, location: Location,
			controller: Controller,
			base_controller: BaseController,
			type_specifics: new TypeSpecifics.spell(new(
				base_cost: BaseCost, cost: Cost, is_class_ability: IsClassAbility,
				can_be_class_ability: CanBeClassAbility)));
	}
}

internal abstract class Quest(string Name, string Text, int ProgressGoal, PlayerClass CardClass) : Card(
	CardClass: CardClass,
	Name: Name,
	Text: Text)
{
	private int _progress;
	public int Progress
	{
		get => _progress;
		set => _progress = Math.Min(value, Goal);
	}
	public readonly int Goal = ProgressGoal;

	public abstract void Reward();

	public override CardStruct ToStruct(bool client = false)
	{
		return new CardStruct(name: Name,
			text: Text,
			card_class: CardClass,
			uid: uid, location: Location,
			controller: Controller,
			base_controller: BaseController,
			type_specifics: new TypeSpecifics.quest(new(progress: Progress, goal: Goal)));
	}
}

internal class Token : Creature
{
	public Token(string Name,
		string Text,
		int OriginalCost,
		int OriginalLife,
		int OriginalPower,
		int OriginalController) : base(
			Name: Name,
			Text: Text,
			OriginalCost: OriginalCost,
			OriginalLife: OriginalLife,
			OriginalPower: OriginalPower,
			CardClass: PlayerClass.All
		)
	{
		BaseController = OriginalController;
		RegisterKeyword(Keyword.Token);
		ResetToBaseState();
	}
	public override void Init()
	{
	}
}
