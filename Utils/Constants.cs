using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CardGameUtils;


[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(PlatformCoreConfig))]
[JsonSerializable(typeof(CoreConfig))]
internal partial class PlatformCoreConfigSerializationContext : JsonSerializerContext { }


[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(PlatformClientConfig))]
internal partial class PlatformClientConfigSerializationContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(PlatformServerConfig))]
internal partial class PlatformServerConfigSerializationContext : JsonSerializerContext { }

internal class GameConstants
{
	public const int SERVER_PORT = 7043;
	public const int MAX_CARD_MULTIPLICITY = 2;
	public const int DECK_SIZE = 40;
	public const int START_HAND_SIZE = 5;
	public const int START_LIFE = 40;
	public const int START_MOMENTUM = 2;
	public const int MAX_HAND_SIZE = 7;
	public static readonly int[] MOMENTUM_INCREMENT_TURNS = [2, 4, 6, 8];
	public const int FIELD_SIZE = 6;

	internal enum State
	{
		UNINITIALIZED,
		TurnStart,
		MainStart,
		BattleStart,
		DamageCalc,
		TurnEnd,
		BattleInitGained = BattleStart + InitGained,
		BattleActionTaken = BattleStart + ActionTaken,
		MainInitGained = MainStart + InitGained,
		MainActionTaken = MainStart + ActionTaken,
		ActionTaken = 0x1000,
		InitGained = 0x10000,
	}
}

internal class ClientConstants
{
	public static readonly Dictionary<string, string> KeywordDescriptions = new()
	{
		{ "Mighty", "Excess combat damage gets dealt as Damage unless the opposing creature has Mighty" },
		{ "Brittle", "The creature dies at the end of the turn" },
		{ "Colossal", "The creature needs X additional momentum to move" },
		{ "Decaying", "The creature loses 1 Life at the end of the turn" },
		{ "Gather", "Look at the top X cards of your deck, add 1 of them to your hand" },
		{ "Immovable", "This creature can not be moved" },
	};
}
