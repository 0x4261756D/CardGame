//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using CardGameUtils.GameConstants;

class Annihilate : Spell
{
	public Annihilate() : base(
		Name: "Annihilate",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 3,
		Text: "{Cast}: Destroy target creature. You take target's power as damage."
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
