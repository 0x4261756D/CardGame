// Scripted by Dotlof
using CardGameCore;
using static CardGameCore.CardUtils;
using static CardGameUtils.GameConstants;

class ToBattle : Spell
{
	public ToBattle() : base(
		Name: "To Battle",
		CardClass: PlayerClass.Gladiator,
		OriginalCost: 0,
		Text: "{Cast}: Create three 1/1 Soldier token with {Attack}: gain +1/+1.\n{Revelation}: Create a 1/1 Soldier Token."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new Trigger(effect: CastEffect, condition: CastCondition), referrer: this);
		RegisterRevelationTrigger(trigger: new Trigger(effect: RevelationEffect, RevealCondition), referrer: this);
	}

	public void RevelationEffect()
	{
		CreateTokenOnField(player: Controller, power: 1, life: 1, name: "Soldier", source: this);
	}

	public void CastEffect()
	{
		for(int i = 0; i <= 2; i++)
		{
			Token token = CreateToken(player: Controller, power: 1, life: 1, name: "Soldier");
			RegisterAttackTrigger(trigger: new CreatureTargetingTrigger(effect: AttackEffect, influenceLocation: Location.Field), referrer: token);
			MoveToField(targetPlayer: Controller, choosingPlayer: Controller, card: token, source: this);
		}
	}

	public void AttackEffect(Creature token)
	{
		RegisterLingeringEffect(info: LingeringEffectInfo.Create(effect: Buff, referrer: token));
	}

	public static void Buff(Creature token)
	{
		token.Life += 1;
		token.Power += 1;
	}

	public bool CastCondition()
	{
		return FIELD_SIZE - GetFieldUsed(Controller).Length > 2;
	}

	public bool RevealCondition()
	{
		return HasEmpty(GetField(Controller));
	}
}
