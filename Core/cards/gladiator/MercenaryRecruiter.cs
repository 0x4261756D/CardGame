// Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using CardGameUtils.CardConstants;

class MercenaryRecruiter : Creature
{
	public MercenaryRecruiter() : base(
		Name: "Mercenary Recruiter",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 3,
		Text: "{Cast}: [Gather] 5, if the Gathered card is a creature put it into play",
		OriginalPower: 3,
		OriginalLife: 4
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
	}

	private void CastEffect()
	{
		Card target = Gather(player: Controller, amount: 5);
		if(target.CardType == CardType.Creature)
		{
			if(HasEmpty(GetField(Controller)))
			{
				MoveToField(Controller, Controller, (Creature)target, this);
			}
		}
	}
}
