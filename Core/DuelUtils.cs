using CardGameUtils;
using CardGameUtils.GameEnumsAndStructs;

namespace CardGameCore;

internal enum Keyword
{
	Colossal,
	Brittle,
	Token,
	Decaying,
	Immovable,
	Mighty,
}

internal class Trigger
{
	public TriggerCondition condition;
	public Effect effect;

	public Trigger(Effect effect, TriggerCondition condition)
	{
		this.effect = effect;
		this.condition = condition;
	}

	public Trigger(Effect effect)
	{
		this.effect = effect;
		this.condition = () => true;
	}

	// NOTE: This is only used for inheritance
	protected Trigger()
	{
		effect = () => { };
		condition = () => true;
	}
}

internal class LocationBasedTrigger
{
	public TriggerCondition condition;
	public Effect effect;
	public Location influenceLocation;

	public LocationBasedTrigger(Effect effect, TriggerCondition condition, Location influenceLocation)
	{
		this.influenceLocation = influenceLocation;
		this.condition = condition;
		this.effect = effect;
	}
	public LocationBasedTrigger(Effect effect, Location influenceLocation)
	{
		this.influenceLocation = influenceLocation;
		this.effect = effect;
		this.condition = () => true;
	}
}
internal class StateReachedTrigger
{
	public Location influenceLocation;
	public GameConstants.State state;
	public TriggerCondition condition;
	public Effect effect;
	public bool oneshot, wasTriggered;

	public StateReachedTrigger(Effect effect, TriggerCondition condition, GameConstants.State state, Location influenceLocation = Location.Field, bool oneshot = false)
	{
		this.influenceLocation = influenceLocation;
		this.state = state;
		this.oneshot = oneshot;
		this.effect = effect;
		this.condition = condition;
	}
	public StateReachedTrigger(Effect effect, GameConstants.State state, Location influenceLocation = Location.Field, bool oneshot = false)
	{
		this.influenceLocation = influenceLocation;
		this.state = state;
		this.oneshot = oneshot;
		this.effect = effect;
		this.condition = () => true;
	}
}
internal class LocationBasedTargetingTrigger
{
	public Location influenceLocation;
	public TargetingCondition condition;
	public TargetingEffect effect;

	public LocationBasedTargetingTrigger(TargetingEffect effect, TargetingCondition condition, Location influenceLocation)
	{
		this.influenceLocation = influenceLocation;
		this.condition = condition;
		this.effect = effect;
	}
	public LocationBasedTargetingTrigger(TargetingEffect effect, Location influenceLocation)
	{
		this.influenceLocation = influenceLocation;
		this.condition = (_) => true;
		this.effect = effect;
	}
}

internal class TokenCreationTrigger
{
	public Location influenceLocation;
	public TokenCreationCondition condition;
	public TokenCreationEffect effect;

	public TokenCreationTrigger(TokenCreationEffect effect, TokenCreationCondition condition, Location influenceLocation)
	{
		this.influenceLocation = influenceLocation;
		this.condition = condition;
		this.effect = effect;
	}
	public TokenCreationTrigger(TokenCreationEffect effect, Location influenceLocation)
	{
		this.influenceLocation = influenceLocation;
		this.condition = (_, _) => true;
		this.effect = effect;
	}
}

internal class CreatureTargetingTrigger(CreatureTargetingEffect effect, CreatureTargetingCondition condition, Location influenceLocation)
{
	public CreatureTargetingCondition condition = condition;
	public CreatureTargetingEffect effect = effect;
	public Location influenceLocation = influenceLocation;

	public CreatureTargetingTrigger(CreatureTargetingEffect effect, Location influenceLocation) : this(effect, (_) => true, influenceLocation) { }
}

internal class LingeringEffectInfo
{
	public static int timestampCounter;
	public int timestamp;
	public TargetingEffect effect;
	public Card referrer;
	public Location influenceLocation;

	internal delegate void SpecificTargetingEffect<T>(T target);
	public static LingeringEffectInfo Create<T>(SpecificTargetingEffect<T> effect, T referrer, Location influenceLocation = Location.Field) where T : Card
	{
		return new LingeringEffectInfo(effect: (target) => effect((T)target), referrer: referrer, influenceLocation: influenceLocation);
	}
	private LingeringEffectInfo(TargetingEffect effect, Card referrer, Location influenceLocation)
	{
		this.effect = effect;
		this.referrer = referrer;
		this.influenceLocation = influenceLocation;
	}
}

internal delegate bool ActivatedEffectCondition();
internal class ActivatedEffectInfo
{
	public ActivatedEffectCondition condition;
	public Effect effect;
	public string name;
	public Location influenceLocation;
	public Card referrer;
	public int uses, maxUses;
	public uint cardActionUid;

	public ActivatedEffectInfo(string name, Effect effect, ActivatedEffectCondition condition, Card referrer, int maxUses = 1, Location influenceLocation = Location.Field)
	{
		this.condition = condition;
		this.effect = effect;
		this.name = name;
		this.influenceLocation = influenceLocation;
		this.referrer = referrer;
		this.maxUses = maxUses;
		this.cardActionUid = DuelCore.CardActionUIDCount;
		DuelCore.CardActionUIDCount += 1;
	}

	public ActivatedEffectInfo(string name, Effect effect, Card referrer, int maxUses = 1, Location influenceLocation = Location.Field)
		: this(name, effect, () => true, referrer, maxUses, influenceLocation)
	{
	}

	public bool CanActivate(Location location)
	{
		return uses < maxUses && Functions.IsInLocation(influenceLocation, location) && Functions.IsInLocation(referrer.Location, location) && condition();
	}
}

internal delegate bool TriggerCondition();
internal delegate void Effect();
internal delegate void TargetingEffect(Card target);
internal delegate bool TargetingCondition(Card target);
internal delegate bool CreatureTargetingCondition(Creature target);
internal delegate void CreatureTargetingEffect(Creature target);
internal delegate void TokenCreationEffect(Creature token, Card source);
internal delegate bool TokenCreationCondition(Creature token, Card source);
internal delegate void RemoveLingeringEffectDelegate(LingeringEffectInfo info);
internal delegate void RegisterLocationBasedTargetingTriggerDelegate(LocationBasedTargetingTrigger trigger, Card referrer);
internal delegate void RegisterTokenCreationTriggerDelegate(TokenCreationTrigger trigger, Card referrer);
internal delegate void RegisterTriggerDelegate(Trigger trigger, Card referrer);
internal delegate void RegisterLocationBasedTriggerDelegate(LocationBasedTrigger trigger, Card referrer);
internal delegate void RegisterStateReachedTriggerDelegate(StateReachedTrigger trigger, Card referrer);
internal delegate void RegisterCreatureTargetingTriggerDelegate(CreatureTargetingTrigger trigger, Card referrer);
internal delegate void RegisterLingeringEffectDelegate(LingeringEffectInfo info);
internal delegate void RegisterStateTemporaryLingeringEffectDelegate(LingeringEffectInfo info, GameConstants.State state);
internal delegate void RegisterActivatedEffectDelegate(ActivatedEffectInfo info);
internal delegate void CastDelegate(int player, Card card);
internal delegate void DrawDelegate(int player, int amount);
internal delegate void SetDamageMultiplierDelegate(int value);
internal delegate int GetDamageMultiplierDelegate();
internal delegate Card[] GetCardsInLocationDelegate(int player);
internal delegate Creature[] GetFieldUsedDelegate(int player);
internal delegate Creature?[] GetWholeFieldDelegate(int player);
internal delegate Card[] SelectCardsDelegate(int player, Card[] cards, uint amount, string description);
internal delegate void DiscardDelegate(Card card);
internal delegate void DiscardAmountDelegate(int player, uint amount);
internal delegate void CreateTokenOnFieldDelegate(int player, int power, int life, string name, Card source);
internal delegate Token CreateTokenDelegate(int player, int power, int life, string name);
internal delegate Creature CreateTokenCopyDelegate(int player, Creature card);
internal delegate void CreateTokenCopyOnFieldDelegate(int player, Creature card, Card source);
internal delegate int GetYXTurnsAgoDelegate(int player, int turns);
internal delegate void CreatureChangeStatDelegate(Creature target, int amount, Card source);
internal delegate void PlayerChangeLifeDelegate(int player, int amount, Card source);
internal delegate void PlayerChangeMomentumDelegate(int player, int amount);
internal delegate void DestroyDelegate(Creature card);
internal delegate bool AskYesNoDelegate(int player, string question);
internal delegate int GetIgniteDamageDelegate(int player);
internal delegate void ChangeIgniteDamageDelegate(int player, int amount);
internal delegate int GetTurnDelegate();
internal delegate int GetPlayerLifeDelegate(int player);
internal delegate void PayLifeDelegate(int player, int amount);
internal delegate Card GatherDelegate(int player, int amount);
internal delegate void MoveDelegate(Creature card, int zone);
internal delegate int SelectZoneDelegate(int choosingPlayer, int targetPlayer);
internal delegate void MoveToHandDelegate(int player, Card card);
internal delegate void MoveToFieldDelegate(int choosingPlayer, int targetPlayer, Creature card, Card source);
internal delegate int GetCastCountDelegate(int player, string name);
internal delegate void ReturnCardsToDeckDelegate(Card[] cards);
internal delegate void RevealDelegate(int player, int damage);
internal delegate Card[] GetDiscardableDelegate(int player, Card? ignore);
internal delegate void RefreshAbilityDelegate(int player);
