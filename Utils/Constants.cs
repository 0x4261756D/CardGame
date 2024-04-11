using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CardGameUtils.Structs;

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

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(NetworkingStructs.Packet))]
internal partial class PacketSerializationContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(Replay))]
internal partial class ReplaySerializationContext : JsonSerializerContext { }

public class GenericConstants
{
	public const uint PACKET_VERSION = 6;


	public static readonly JsonSerializerOptions platformClientConfigSerialization = new()
	{
		TypeInfoResolver = PlatformClientConfigSerializationContext.Default,
		IncludeFields = true,
		WriteIndented = true,
	};
	public static readonly JsonSerializerOptions platformCoreConfigSerialization = new()
	{
		TypeInfoResolver = PlatformCoreConfigSerializationContext.Default,
		IncludeFields = true,
	};
	public static readonly JsonSerializerOptions platformServerConfigSerialization = new()
	{
		TypeInfoResolver = PlatformServerConfigSerializationContext.Default,
		IncludeFields = true,
	};
	public static readonly JsonSerializerOptions packetSerialization = new()
	{
		TypeInfoResolver = PacketSerializationContext.Default,
		IncludeFields = true,
	};
	public static readonly JsonSerializerOptions replaySerialization = new()
	{
		TypeInfoResolver = ReplaySerializationContext.Default,
		IncludeFields = true,
	};
}

public class GameConstants
{
	public const int MAX_CARD_MULTIPLICITY = 2;
	public const int DECK_SIZE = 40;
	public const int START_HAND_SIZE = 5;
	public const int START_LIFE = 40;
	public const int START_MOMENTUM = 2;
	public const int MAX_HAND_SIZE = 7;
	public static readonly int[] MOMENTUM_INCREMENT_TURNS = [2, 4, 6, 8];
	public const int FIELD_SIZE = 6;

	public enum State
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
	public enum CardType
	{
		UNKNOWN,
		Creature,
		Spell,
		Quest,
	}

	public enum GameResult
	{
		Draw,
		Won,
		Lost,
	}

	public enum PlayerClass
	{
		UNKNOWN,
		All,
		Cultist,
		Pyromancer,
		Artificer,
		Gladiator
	}

	[Flags]
	public enum Location
	{
		UNKNOWN,
		Deck = 1 << 0,
		Hand = 1 << 1,
		Field = 1 << 2,
		Grave = 1 << 3,
		Quest = 1 << 4,
		Ability = 1 << 5,
		ALL = Deck | Hand | Field | Grave,
	}
}

public class ClientConstants
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
