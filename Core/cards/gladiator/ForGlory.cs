// Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;

class ForGlory : Quest
{
	public ForGlory() : base(
		Name: "For Glory!",
		CardClass: PlayerClass.Gladiator,
		ProgressGoal: 10,
		Text: "{Victorious}: Gain 1 progress.\n{Reward}: Allied creatures gain +1/+1 and [Mighty]."
		)
	{ }

	public override void Init()
	{
		RegisterGenericVictoriousTrigger(trigger: new CreatureTargetingTrigger(effect: ProgressEffect, influenceLocation: Location.Quest), referrer: this);
	}

	private void ProgressEffect(Card _)
	{
		Progress++;
	}

	public override void Reward()
	{
		RegisterLingeringEffect(info: LingeringEffectInfo.Create(effect: RewardEffect, referrer: this, influenceLocation: Location.Quest));
	}

	private void RewardEffect(Card _)
	{
		foreach(Creature card in GetFieldUsed(Controller))
		{
			card.RegisterKeyword(Keyword.Mighty);
			card.Life += 1;
			card.Power += 1;
		}
	}
}
