//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using CardGameUtils.GameConstants;

class Casualties : Spell
{
	public Casualties() : base(
		Name: "Casualties",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 4,
		Text: "{Cast}: Destroy all creatures except the ones with the highest power."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
	}

	private void CastEffect()
	{
		int highestPower = 0;
		foreach(Creature creature in GetBothFieldsUsed())
		{
			if(creature.Power > highestPower)
			{
				highestPower = creature.Power;
			}
		}
		foreach(Creature creature in GetBothFieldsUsed())
		{
			if(creature.Power < highestPower)
			{
				Destroy(creature);
			}
		}
	}
}
