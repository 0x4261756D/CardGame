// Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;

class RagingBeast : Creature
{
	public RagingBeast() : base(
		Name: "Raging Beast",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 4,
		Text: "[Mighty] {Victorious}: Draw 1 and gain +1/+1",
		OriginalPower: 7,
		OriginalLife: 4
		)
	{ }

	public override void Init()
	{
		RegisterKeyword(Keyword.Mighty, 0);
        RegisterVictoriousTrigger(trigger: new Trigger(effect: VictoriousEffect), referrer: this);
	}

    private void VictoriousEffect()
    {
        Power += 1;
        Life += 1;
        Draw(player: Controller, amount: 1);
    }
}
