// Scripted by 0x4261756D
using CardGameCore;
using CardGameUtils.Constants;

class AnimatedWorkbench : Creature
{
	public AnimatedWorkbench() : base(
		Name: "Animated Workbench",
		CardClass: PlayerClass.Artificer,
		OriginalCost: 2,
		Text: "{Turn start}: Gain 1 Momentum.",
		OriginalPower: 1,
		OriginalLife: 3
		)
	{ }

	public override void Init()
	{
		RegisterStateReachedTrigger(new StateReachedTrigger(effect: TriggerEffect, state: GameConstants.GameState.TurnStart), referrer: this);
	}

	public void TriggerEffect()
	{
		PlayerChangeMomentum(player: Controller, amount: 1);
	}
}
