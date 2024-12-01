//Scripted by Dotlof
using CardGameCore;
using CardGameUtils.GameEnumsAndStructs;
using static CardGameUtils.GameConstants;

class RiskyGambit : Spell
{
	public RiskyGambit() : base(
		Name: "Risky Gambit",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: All damage this turn is doubled.\n{Revelation}: Pay 5 Life. Draw 1."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
		RegisterRevelationTrigger(trigger: new Trigger(effect: RevelationEffect), referrer: this);
	}

	public void RevelationEffect()
	{
		PlayerChangeLife(player: Controller, amount: -5, source: this);
		Draw(player: Controller, amount: 1);
	}


	private void CastEffect()
	{
		SetDamageMultiplier(GetDamageMultiplier() * 2);
		RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: EndPhaseEffect, state: State.TurnEnd, influenceLocation: Location.Any, oneshot: true), referrer: this);
	}

	private static void EndPhaseEffect()
	{
		SetDamageMultiplier(GetDamageMultiplier() / 2);
	}
}
