//Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;

class TitanicMight : Spell
{
	public TitanicMight() : base(
		Name: "Titanic Might",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 6,
		Text: "{Cast}: Allied creatures gain +7/+7 and [Mighty]."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
	}

	private void CastEffect()
	{
		foreach(Creature creature in GetFieldUsed(Controller))
		{
			creature.RegisterKeyword(Keyword.Mighty);
			RegisterLingeringEffect(LingeringEffectInfo.Create(effect: BuffEffect, referrer: creature));
		}
	}

    private void BuffEffect(Creature creature){
        creature.Power += 7;
        creature.Life += 7;
    }

}
