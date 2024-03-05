//Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;
using static CardGameCore.CardUtils;
using System.Collections.Generic;

class Warsong : Spell
{
	public Warsong() : base(
		Name: "Warsong",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: Allied Creatures gain +3/+3 and [Mighty] this turn. At the end of your turn they again +1/+1.\n{Revelation}: Target allied creature gains +1/+1"
		)
	{ }

	private readonly List<LingeringEffectInfo> temp = [];

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
			creature.RegisterKeyword(Keyword.Mighty);
			LingeringEffectInfo info = LingeringEffectInfo.Create(effect: BigBuff, referrer: creature);
			temp.Add(info);
			RegisterLingeringEffect(info);
		}
		RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: EndPhaseEffect, state: State.TurnEnd, influenceLocation: Location.ALL, oneshot: true), referrer: this);
	}

	private void EndPhaseEffect()
	{
		foreach(Creature creature in GetFieldUsed(Controller))
		{
			RegisterLingeringEffect(info: LingeringEffectInfo.Create(effect: SmallBuff, referrer: creature));
		}
		foreach(LingeringEffectInfo info in temp)
		{
			RemoveLingeringEffect(info);
			_ = ((Creature)info.referrer).Keywords.Remove(Keyword.Mighty);
		}
		temp.Clear();
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
