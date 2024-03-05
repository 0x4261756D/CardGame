// Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;
using static CardGameCore.CardUtils;

class LegionChampion : Creature
{
	public LegionChampion() : base(
		Name: "Legion Champion",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 6,
		Text: "[Mighty]\n{Attack}: Draw 1.\n{Victorious}: Deal X damage to any target, where X is the highest power among allied creatures.",
		OriginalPower: 9,
		OriginalLife: 7
		)
	{ }

	public override void Init()
	{
		RegisterKeyword(Keyword.Mighty);
		RegisterAttackTrigger(trigger: new CreatureTargetingTrigger(effect: AttackEffect, influenceLocation: Location.Field), referrer: this);
		RegisterVictoriousTrigger(trigger: new Trigger(effect: VictoriousEffect), referrer: this);
	}

	private void VictoriousEffect()
	{
		int highestPower = 0;
		foreach(Creature creature in GetFieldUsed(Controller))
		{
			if(creature.Power > highestPower)
			{
				highestPower = creature.Power;
			}
		}
		ChangeLifeOfAnyTarget(player: Controller, amount: -highestPower, description: "Legion Champion Victorious Effect", source: this);
	}

	private void AttackEffect(Creature _)
	{
		Draw(player: Controller, amount: 1);
	}
}
