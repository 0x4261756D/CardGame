//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using CardGameUtils.GameConstants;

class Recruit : Spell
{
	public Recruit() : base(
		Name: "Recruit",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 2,
		Text: "{Cast}: [Gather] 5, if the gathered card is a creature put it into play."
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
