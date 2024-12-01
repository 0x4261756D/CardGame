// Scripted by Dotlof
using CardGameCore;
using CardGameUtils.GameEnumsAndStructs;
using static CardGameCore.CardUtils;

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
		if(target is Creature creature)
		{
			if(HasEmpty(GetField(Controller)))
			{
				MoveToField(Controller, Controller, creature, this);
			}
		}
	}
}
