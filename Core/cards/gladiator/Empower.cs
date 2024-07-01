// Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using static CardGameUtils.GameConstants;

class Empower : Spell
{
	public Empower() : base(
		Name: "Empower",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 0,
		Text: "{Cast}: Target allied creature gains +1/+1. It also gains [Mighty] until the end of the turn.\n{Revelation}: Cast this.",
		CanBeClassAbility: true
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect, CastCondition), referrer: this);
		RegisterRevelationTrigger(trigger: new Trigger(effect: RevelationEffect, CastCondition), referrer: this);
	}

	public void RevelationEffect()
	{
		Cast(Controller, this);
	}

	public void CastEffect()
	{
		Creature target = SelectSingleCard(Controller, GetFieldUsed(Controller), "Select target for Empower");
		RegisterLingeringEffect(LingeringEffectInfo.Create(Buff, target));
		target.RegisterKeyword(Keyword.Mighty);
		RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: () => target.Keywords.Remove(Keyword.Mighty), state: State.TurnEnd, influenceLocation: Location.Any, oneshot: true), referrer: this);
	}

	private void Buff(Creature target)
	{
		target.Power += 1;
		target.Life += 1;
	}

	private bool CastCondition()
	{
		return HasUsed(GetField(Controller));
	}

}
