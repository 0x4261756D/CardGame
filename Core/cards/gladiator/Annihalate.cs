//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using static CardGameUtils.GameConstants;

class Annihalate : Spell
{
	public Annihalate() : base(
		Name: "Annihalate",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 3,
		Text: "{Cast}: Destroy target Creature. You take targets attack as damage."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect, condition: CastCondition), referrer: this);
	}

    private bool CastCondition()
	{
		return HasUsed(GetField(1 - Controller));
	}

	private void CastEffect()
	{
        Creature target = SelectSingleCard(player: Controller, cards: GetFieldUsed(1 - Controller), description: "Select creature to destroy");
		Destroy(target);
		PlayerChangeLife(player: Controller, amount: -target.Power, source: this);
	}
}