// Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;

class RagingTitan : Creature
{
	public RagingTitan() : base(
		Name: "Raging Titan",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 7,
		Text: "[Colossal] +1\nCosts X less where X is the highest Power among allied creatures.\n{Revelation}: Gain 3 life.",
		OriginalPower: 7,
		OriginalLife: 7
		)
	{ }

	public override void Init()
	{
		RegisterKeyword(Keyword.Colossal, 1);
		RegisterLingeringEffect(info: LingeringEffectInfo.Create(effect: CostReductionEffect, referrer: this, influenceLocation: Location.Hand));
		RegisterRevelationTrigger(trigger: new Trigger(effect: GainLifeEffect), referrer: this);
	}

	private void GainLifeEffect()
	{
		PlayerChangeLife(player: Controller, amount: 3, source: this);
	}

	private void CostReductionEffect(Creature _)
	{
		//I don't know how often this is checked but could cause problems if it has to iterate over full boards a lot
		int highestPower = 0;
		foreach(Creature creature in GetFieldUsed(Controller))
		{
			if(creature.Power > highestPower)
			{
				highestPower = creature.Power;
			}
		}
		Cost -= highestPower;
	}

}
