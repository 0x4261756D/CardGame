// Scripted by 0x4261756D
using CardGameCore;
using CardGameUtils.GameEnumsAndStructs;

class CourageousKnight : Creature
{
	public CourageousKnight() : base(
		Name: "Courageous Knight",
		CardClass: PlayerClass.All,
		OriginalCost: 3,
		Text: "{A creature enters the field}: Gain +1/+0.",
		OriginalPower: 0,
		OriginalLife: 4
		)
	{ }

	public override void Init()
	{
		RegisterGenericEntersFieldTrigger(trigger: new LocationBasedTargetingTrigger(effect: BuffEffect, influenceLocation: Location.Field), referrer: this);
	}

	public void BuffEffect(Card castCard)
	{
		if(castCard is Creature)
		{
			CreatureChangePower(target: this, amount: 1, source: this);
		}
	}

}
