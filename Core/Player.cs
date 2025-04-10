using System.Collections.Generic;
using CardGameUtils.GameEnumsAndStructs;
using CardGameUtils;

namespace CardGameCore;

class Player
{
	public Deck deck;
	public Grave grave = new();
	public Field field = new();
	public Hand hand = new();
	public string id;
	public int number;
	public string name;
	public bool passed;
	public bool abilityUsable = true;
	public PlayerClass playerClass;
	public Spell ability;
	public Quest quest;
	public int life, momentum;
	public List<int> discardCounts = [], dealtDamages = [], dealtSpellDamages = [], brittleDeathCounts = [], deathCounts = [];
	public int baseIgniteDamage = 1, igniteDamage;
	public Dictionary<string, int> castCounts = [];
	public Player(CoreConfig.PlayerConfig config, int number, Deck deck, PlayerClass playerClass, Spell ability, Quest quest)
	{
		this.deck = deck;
		this.id = config.id;
		this.name = config.name;
		this.passed = false;
		this.playerClass = playerClass;
		this.ability = ability;
		this.ability.Location = Location.Ability;
		this.quest = quest;
		this.quest.Location = Location.Quest;
		this.number = number;
		ClearCardModifications();
	}

	internal void Draw(int amount)
	{
		for(int i = 0; i < amount; i++)
		{
			Card? card = deck.Pop();
			if(card == null)
			{
				life -= 1;
			}
			else
			{
				hand.Add(card);
			}
		}
	}

	internal void ClearCardModifications()
	{
		igniteDamage = baseIgniteDamage;
		hand.ClearCardModifications();
		field.ClearCardModifications();
	}
}
