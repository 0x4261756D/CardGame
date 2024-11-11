//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using CardGameUtils.GameConstants;

class Rush : Spell
{
	public Rush() : base(
		Name: "Rush",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: Deal X damage to any target where X is the highest power among allied creatures."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect, condition: CastCondition), referrer: this);
	}

	private bool CastCondition()
	{
		return HasUsed(GetField(Controller));
	}

	private void CastEffect()
	{
		int damage = 0;
		foreach(Creature creature in GetFieldUsed(Controller))
		{
			if(creature.Power > damage)
			{
				damage = creature.Power;
			}
		}
		ChangeLifeOfAnyTarget(player: Controller, amount: -damage, description: "Rush", source: this);
	}
}
