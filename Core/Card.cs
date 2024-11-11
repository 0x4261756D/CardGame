using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CardGameUtils.GameConstants;
using CardGameUtils.Base;

namespace CardGameCore;

public abstract class Card
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

	public static RemoveLingeringEffectDelegate RemoveLingeringEffect = (_) => { };
	public static GetDamageMultiplierDelegate GetDamageMultiplier = () => 1;
	public static SetDamageMultiplierDelegate SetDamageMultiplier = (_) => { };
	public static RegisterTriggerDelegate RegisterCastTrigger = (_, _) => { };
	public static RegisterLocationBasedTargetingTriggerDelegate RegisterGenericCastTrigger = (_, _) => { };
	public static RegisterTokenCreationTriggerDelegate RegisterTokenCreationTrigger = (_, _) => { };
	public static RegisterLocationBasedTargetingTriggerDelegate RegisterGenericEntersFieldTrigger = (_, _) => { };
	public static RegisterTriggerDelegate RegisterRevelationTrigger = (_, _) => { };
	public static RegisterLocationBasedTriggerDelegate RegisterYouDiscardTrigger = (_, _) => { };
	public static RegisterTriggerDelegate RegisterDiscardTrigger = (_, _) => { };
	public static RegisterStateReachedTriggerDelegate RegisterStateReachedTrigger = (_, _) => { };
	public static RegisterTriggerDelegate RegisterVictoriousTrigger = (_, _) => { };
	public static RegisterCreatureTargetingTriggerDelegate RegisterAttackTrigger = (_, _) => { };
	public static RegisterCreatureTargetingTriggerDelegate RegisterDeathTrigger = (_, _) => { };
	public static RegisterCreatureTargetingTriggerDelegate RegisterGenericDeathTrigger = (_, _) => { };
	public static RegisterCreatureTargetingTriggerDelegate RegisterGenericVictoriousTrigger = (_, _) => { };
	public static RegisterTriggerDelegate RegisterDealsDamageTrigger = (_, _) => { };
	public static RegisterLingeringEffectDelegate RegisterLingeringEffect = (_) => { };
	public static RegisterLingeringEffectDelegate RegisterLocationTemporaryLingeringEffect = (_) => { };
	public static RegisterStateTemporaryLingeringEffectDelegate RegisterStateTemporaryLingeringEffect = (_, _) => { };
	public static RegisterActivatedEffectDelegate RegisterActivatedEffect = (_) => { };
	public static GetCardsInLocationDelegate GetGrave = (_) => [];
	public static GetWholeFieldDelegate GetField = (_) => [];
	public static GetFieldUsedDelegate GetFieldUsed = (_) => [];
	public static GetCardsInLocationDelegate GetHand = (_) => [];
	public static SelectCardsDelegate SelectCards = (_, _, _, _) => [];
	public static DiscardDelegate Discard = (_) => { };
	public static DiscardAmountDelegate DiscardAmount = (_, _) => { };
	public static CreateTokenDelegate CreateToken = (_, _, _, _) => new ClientCoreDummyToken();
	public static CreateTokenOnFieldDelegate CreateTokenOnField = (_, _, _, _, _) => { };
	public static CreateTokenCopyDelegate CreateTokenCopy = (_, _) => new ClientCoreDummyToken();
	public static CreateTokenCopyOnFieldDelegate CreateTokenCopyOnField = (_, _, _) => { };
	public static GetYXTurnsAgoDelegate GetDiscardCountXTurnsAgo = (_, _) => -1;
	public static GetYXTurnsAgoDelegate GetDamageDealtXTurnsAgo = (_, _) => -1;
	public static GetYXTurnsAgoDelegate GetSpellDamageDealtXTurnsAgo = (_, _) => -1;
	public static GetYXTurnsAgoDelegate GetBrittleDeathCountXTurnsAgo = (_, _) => -1;
	public static GetYXTurnsAgoDelegate GetDeathCountXTurnsAgo = (_, _) => -1;
	public static PlayerChangeLifeDelegate PlayerChangeLife = (_, _, _) => { };
	public static PlayerChangeMomentumDelegate PlayerChangeMomentum = (_, _) => { };
	public static CastDelegate Cast = (_, _) => { };
	public static DrawDelegate Draw = (_, _) => { };
	public static DestroyDelegate Destroy = (_) => { };
	public static AskYesNoDelegate AskYesNo = (_, _) => false;
	public static GetIgniteDamageDelegate GetIgniteDamage = (_) => -1;
	public static ChangeIgniteDamageDelegate ChangeIgniteDamage = (_, _) => { };
	public static ChangeIgniteDamageDelegate ChangeIgniteDamageTemporary = (_, _) => { };
	public static GetTurnDelegate GetTurn = () => -1;
	public static GetPlayerLifeDelegate GetPlayerLife = (_) => -1;
	public static PayLifeDelegate PayLife = (_, _) => { };
	public static GatherDelegate Gather = (_, _) => new ClientCoreDummyCard();
	public static MoveDelegate Move = (_, _) => { };
	public static SelectZoneDelegate SelectZone = (_, _) => -1;
	public static MoveToHandDelegate MoveToHand = (_, _) => { };
	public static MoveToFieldDelegate MoveToField = (_, _, _, _) => { };
	public static GetCastCountDelegate GetCastCount = (_, _) => -1;
	public static ReturnCardsToDeckDelegate ReturnCardsToDeck = (_) => { };
	public static RevealDelegate Reveal = (_, _) => { };
	public static GetDiscardableDelegate GetDiscardable = (_, _) => [];
	public static RefreshAbilityDelegate RefreshAbility = (_) => { };
	public static CreatureChangeStatDelegate CreatureChangeLife = (_, _, _) => { };
	public static CreatureChangeStatDelegate CreatureChangePower = (_, _, _) => { };
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
public class ClientCoreDummyCard : Card
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
public class ClientCoreDummyToken : Token
{
	public ClientCoreDummyToken() : base("UNINITIALIZED", "UNINITIALIZED", -1, -1, -1, -1)
	{ }
	public override void Init()
	{
	}
}

public abstract partial class Creature : Card
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

public abstract class Spell(PlayerClass CardClass,
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

public abstract class Quest(string Name, string Text, int ProgressGoal, PlayerClass CardClass) : Card(
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

public class Token : Creature
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
