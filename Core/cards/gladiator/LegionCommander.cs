// Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;
using static CardGameCore.CardUtils;

class LegionCommander : Creature
{
	public LegionCommander() : base(
		Name: "Legion Commander",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 2,
		Text: "Allied creatures gain +2/+2.\n{Revelation}: Create a 1/1 Soldier Token.",
		OriginalPower: 2,
		OriginalLife: 4
		)
	{ }

	public override void Init()
	{
        RegisterLingeringEffect(LingeringEffectInfo.Create(BuffEffect, this));
        RegisterRevelationTrigger(trigger: new Trigger(effect: RevelationEffect, RevealCondition), referrer: this);
	}

    private void BuffEffect(Creature target)
	{
		foreach(Creature card in GetFieldUsed(target.Controller))
		{
			if(card != target)
			{
				card.Power += 1;
				card.Life += 1;
			}
		}
	}

    public void RevelationEffect()
	{
        CreateTokenOnField(player: Controller, power: 1, life: 1, name: "Soldier", source: this);
	}

    public bool RevealCondition()
	{
		return HasEmpty(GetField(Controller));
	}
}
