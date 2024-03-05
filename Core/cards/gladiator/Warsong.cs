//Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;
using static CardGameCore.CardUtils;

class Warsong : Spell
{
	public Warsong() : base(
		Name: "Warsong",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: Allied creatures gain +3/+3 and [Mighty] this turn. At the end of your turn they again +1/+1.\n{Revelation}: Target allied creature gains +1/+1"
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
		RegisterRevelationTrigger(trigger: new Trigger(effect: RevelationEffect, condition: RevelationCondition), referrer: this);
	}

	public void RevelationEffect()
	{
		Creature target = SelectSingleCard(Controller, GetFieldUsed(Controller), "Select target to gain +1/+1");
		RegisterLingeringEffect(info: LingeringEffectInfo.Create(effect: SmallBuff, referrer: target));
	}


	private void CastEffect()
	{
		foreach(Creature creature in GetFieldUsed(Controller))
		{
			if(!creature.Keywords.ContainsKey(Keyword.Mighty))
			{
				creature.RegisterKeyword(Keyword.Mighty);
				RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: () => creature.Keywords.Remove(Keyword.Mighty), oneshot: true, state: State.TurnEnd), referrer: creature);
			}
			RegisterStateTemporaryLingeringEffect(LingeringEffectInfo.Create(effect: BigBuff, referrer: creature), State.TurnEnd);
			RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: () => RegisterLocationTemporaryLingeringEffect(info: LingeringEffectInfo.Create(effect: SmallBuff, referrer: creature)), state: State.TurnEnd, influenceLocation: Location.Field, oneshot: true), referrer: creature);
		}
	}

	private void BigBuff(Creature creature)
	{
		creature.Life += 3;
		creature.Power += 3;
	}

	private void SmallBuff(Creature creature)
	{
		creature.Life += 1;
		creature.Power += 1;
	}

	private bool RevelationCondition()
	{
		return GetFieldUsed(Controller).Length > 0;
	}
}
