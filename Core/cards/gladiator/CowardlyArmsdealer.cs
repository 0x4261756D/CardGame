// Scripted by Dotlof
using CardGameCore;
using CardGameUtils.GameEnumsAndStructs;
using static CardGameCore.CardUtils;

class CowardlyArmsdealer : Creature
{
	public CowardlyArmsdealer() : base(
		Name: "Cowardly Armsdealer",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: Target allied creature gains +0/+2.\n{Activate}: Return this card to your hand.",
		OriginalPower: 1,
		OriginalLife: 3
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
		RegisterActivatedEffect(info: new ActivatedEffectInfo(name: "Activate", effect: () => MoveToHand(Controller, this), referrer: this));
	}

	private void CastEffect()
	{
		Creature target = SelectSingleCard(Controller, GetFieldUsed(Controller), "Select creature to get +0/+2");
		RegisterLingeringEffect(LingeringEffectInfo.Create(Buff, target));
	}

	private void Buff(Creature target)
	{
		target.Life += 2;
	}
}
