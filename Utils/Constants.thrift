namespace netstd CardGameUtils.Constants

enum GameResult
{
	Draw = 1
	Lost = 2
	Won = 3
}
enum Location
{
	Any = 1
	Deck = 2
	Hand = 3
	Field = 4
	Grave = 5
	Quest = 6
	Ability = 7
}

enum PlayerClass
{
	All = 1
	Cultist = 2
	Pyromancer = 3
	Artificer = 4
	Gladiator = 5
}

struct CardInfo
{
	1: string name;
	2: string text;
	3: PlayerClass card_class;
	4: Location location;
	5: i32 uid;
	6: i32 controller;
	7: i32 base_controller;
	8: CardTypeSpecifics card_type_specifics;
}
union CardTypeSpecifics
{
	1: CreatureSpecifics creature;
	2: SpellSpecifics spell;
	3: QuestSpecifics quest;
}
struct CreatureSpecifics
{
	1: i32 life;
	2: i32 base_life;
	3: i32 power;
	4: i32 base_power;
	5: i32 cost;
	6: i32 base_cost;
	7: i32 position;
}
struct SpellSpecifics
{
	1: i32 cost;
	2: i32 base_cost;
	3: bool is_class_ability;
	4: bool can_be_class_ability;
}
struct QuestSpecifics
{
	1: i32 progress;
	2: i32 goal;
}
