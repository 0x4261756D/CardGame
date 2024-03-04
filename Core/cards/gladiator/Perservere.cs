//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using static CardGameUtils.GameConstants;

class Perservere : Spell
{
    private int oldDamageCap = -1;
	public Perservere() : base(
		Name: "Perservere",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: Target creature is immune to battle damage this turn. {Revelation}: Gain 1 Life."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
	}

	private void CastEffect()
	{
		Creature target = SelectSingleCard(Controller, GetBothFieldsUsed(), "Select target for Perservere");
        oldDamageCap = target.damageCap;
        RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: () => target.damageCap = oldDamageCap, state: State.TurnEnd, influenceLocation: Location.ALL, oneshot: true), referrer: target);
	}

}