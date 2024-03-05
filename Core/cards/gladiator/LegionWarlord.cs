// Scripted by Dotlof
using CardGameCore;
using static CardGameUtils.GameConstants;
using static CardGameCore.CardUtils;

class LegionWarlord : Creature
{
	public LegionWarlord() : base(
		Name: "Legion Warlord",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 4,
		Text: "{Cast}: Create a 1/1 Soldier Token for each card in your hand, then all allied creatures gain +1/+1 for each token you could not create.",
		OriginalPower: 4,
		OriginalLife: 5
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect), referrer: this);
	}

	private void CastEffect()
	{
		for(int i = 0; i < GetHand(Controller).Length; i++)
		{
			if(HasEmpty(GetField(Controller)))
			{
				CreateTokenOnField(Controller, 1, 1, "Soldier", this);
			}
			else
			{
				foreach(Creature creature in GetFieldUsed(Controller))
				{
					RegisterLingeringEffect(LingeringEffectInfo.Create(Buff, creature));
				}
			}
		}
	}

	private void Buff(Creature target)
	{
		target.Power += 1;
		target.Life += 1;
	}
}
