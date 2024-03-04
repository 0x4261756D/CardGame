//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using static CardGameUtils.GameConstants;

class EchoingAnthem : Spell
{
	public EchoingAnthem() : base(
		Name: "Echoing Anthem",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: Allied creatures gain +1/+0 or +0/+1. Return this to your hand. At the end of your turn discard this."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
	}

	private void CastEffect()
	{
		bool buffType = AskYesNo(Controller, "Gain +1/+0?");
		foreach(Creature creature in GetFieldUsed(Controller))
		{
			RegisterLingeringEffect(LingeringEffectInfo.Create(buffType ? PowerBuff : LifeBuff, creature));
		}
		MoveToHand(player: Controller, card: this);
		RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: EndPhaseEffect, condition: EndPhaseCondition, state: State.TurnEnd, influenceLocation: Location.ALL, oneshot: true), referrer: this);
	}


	private bool EndPhaseCondition()
	{
		return ContainsValid(GetHand(Controller), EndPhaseFilter);
	}

	private void EndPhaseEffect()
	{
		Discard(SelectSingleCard(Controller, FilterValid(GetHand(Controller), EndPhaseFilter), description: $"Select \"{Name}\" to discard"));
	}

	private bool EndPhaseFilter(Card card) => card.Name == this.Name;

	private void PowerBuff(Creature target){
		target.Power++;
	}

	private void LifeBuff(Creature target){
		target.Life++;
	}
}
