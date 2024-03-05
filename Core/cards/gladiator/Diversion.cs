//Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using static CardGameUtils.GameConstants;

class Diversion : Spell
{
	public Diversion() : base(
		Name: "Diversion",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 2,
		Text: "{Cast}: You may move each creature to a zone it can occupy."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
	}

	private void CastEffect()
	{
		foreach(Creature creature in GetBothFieldsUsed())
		{
			if(AskYesNo(Controller, $"Move {creature.Name}?"))
			{
				MoveToField(choosingPlayer: Controller, targetPlayer: creature.Controller, card: creature, source: this);
			}
		}
	}
}
