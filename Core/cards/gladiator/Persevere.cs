//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using static CardGameUtils.GameConstants;

class Persevere : Spell
{
	public Persevere() : base(
		Name: "Persevere",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{Cast}: Target creature is immune to damage this turn. {Revelation}: Gain 1 Life."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
		RegisterRevelationTrigger(trigger: new(effect: () => PlayerChangeLife(player: Controller, amount: 1, source: this)), referrer: this);
	}

	private void CastEffect()
	{
		Creature target = SelectSingleCard(Controller, GetBothFieldsUsed(), "Select target for Persevere");
		int oldDamageCap = target.damageCap;
		target.damageCap = -1;
		RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: () => target.damageCap = oldDamageCap, state: State.TurnEnd, influenceLocation: Location.Any, oneshot: true), referrer: target);
	}

}
