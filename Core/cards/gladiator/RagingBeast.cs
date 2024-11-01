// Scripted by Dotlof
using CardGameCore;
using CardGameUtils.GameConstants;

class RagingBeast : Creature
{
	public RagingBeast() : base(
		Name: "Raging Beast",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 4,
		Text: "[Mighty]\n{Victorious}: Draw 1 and gain +1/+1",
		OriginalPower: 7,
		OriginalLife: 4
		)
	{ }

	public override void Init()
	{
		RegisterKeyword(Keyword.Mighty);
		RegisterVictoriousTrigger(trigger: new Trigger(effect: VictoriousEffect), referrer: this);
	}

	private void VictoriousEffect()
	{
		RegisterLocationTemporaryLingeringEffect(info: LingeringEffectInfo.Create((c) => { c.Power += 1; c.Life += 1; }, this));
		Draw(player: Controller, amount: 1);
	}
}
