// Scripted by Dotlof
using CardGameCore;
using CardGameUtils.GameEnumsAndStructs;
using static CardGameUtils.GameConstants;

class LegionScout : Creature
{
	public LegionScout() : base(
		Name: "Legion Scout",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 1,
		Text: "{End of turn}: Draw 1",
		OriginalPower: 1,
		OriginalLife: 2
		)
	{ }

	public override void Init()
	{
		RegisterStateReachedTrigger(new StateReachedTrigger(EndPhaseEffect, State.TurnEnd), this);
	}

	private void EndPhaseEffect()
	{
		Draw(player: Controller, amount: 1);
	}
}
