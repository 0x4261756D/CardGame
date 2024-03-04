using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CardGameUtils;

public class GenericConstants
{
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

public class NetworkingConstants
{
	public enum PacketType : byte
	{
		UNKNOWN,
		DeckNamesRequest,
		DeckNamesResponse,
		DeckListRequest,
		DeckListResponse,
		DeckSearchRequest,
		DeckSearchResponse,
		DeckListUpdateRequest,
		DeckListUpdateResponse,

		DuelSurrenderRequest,
		DuelYesNoRequest,
		DuelYesNoResponse,
		DuelSelectCardsRequest,
		DuelSelectCardsResponse,
		DuelCustomSelectCardsRequest,
		DuelCustomSelectCardsResponse,
		DuelCustomSelectCardsIntermediateRequest,
		DuelCustomSelectCardsIntermediateResponse,
		DuelFieldUpdateRequest,
		DuelGetOptionsRequest,
		DuelGetOptionsResponse,
		DuelSelectOptionRequest,
		DuelSelectZoneRequest,
		DuelSelectZoneResponse,
		DuelPassRequest,
		DuelGameResultResponse,
		DuelViewGraveRequest,
		DuelViewCardsResponse,

		ServerAdditionalCardsRequest,
		ServerAdditionalCardsResponse,
		ServerCreateRequest,
		ServerCreateResponse,
		ServerJoinRequest,
		ServerJoinResponse,
		ServerOpponentChangedResponse,
		ServerLeaveRequest,
		ServerRoomsRequest,
		ServerRoomsResponse,
		ServerStartRequest,
		ServerStartResponse,
		PACKET_COUNT,
	}
	public static readonly Dictionary<Type, byte> PacketDict = new()
	{
		{typeof(Structs.NetworkingStructs.DeckPackets.NamesRequest), (byte)PacketType.DeckNamesRequest},
		{typeof(Structs.NetworkingStructs.DeckPackets.NamesResponse), (byte)PacketType.DeckNamesResponse},
		{typeof(Structs.NetworkingStructs.DeckPackets.ListRequest), (byte)PacketType.DeckListRequest},
		{typeof(Structs.NetworkingStructs.DeckPackets.ListResponse), (byte)PacketType.DeckListResponse},
		{typeof(Structs.NetworkingStructs.DeckPackets.SearchRequest), (byte)PacketType.DeckSearchRequest},
		{typeof(Structs.NetworkingStructs.DeckPackets.SearchResponse), (byte)PacketType.DeckSearchResponse},
		{typeof(Structs.NetworkingStructs.DeckPackets.ListUpdateRequest), (byte)PacketType.DeckListUpdateRequest},
		{typeof(Structs.NetworkingStructs.DeckPackets.ListUpdateResponse), (byte)PacketType.DeckListUpdateResponse},

		{typeof(Structs.NetworkingStructs.DuelPackets.SurrenderRequest), (byte)PacketType.DuelSurrenderRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.YesNoRequest), (byte)PacketType.DuelYesNoRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.YesNoResponse), (byte)PacketType.DuelYesNoResponse},
		{typeof(Structs.NetworkingStructs.DuelPackets.SelectCardsRequest), (byte)PacketType.DuelSelectCardsRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.SelectCardsResponse), (byte)PacketType.DuelSelectCardsResponse},
		{typeof(Structs.NetworkingStructs.DuelPackets.CustomSelectCardsRequest), (byte)PacketType.DuelCustomSelectCardsRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.CustomSelectCardsResponse), (byte)PacketType.DuelCustomSelectCardsResponse},
		{typeof(Structs.NetworkingStructs.DuelPackets.CustomSelectCardsIntermediateRequest), (byte)PacketType.DuelCustomSelectCardsIntermediateRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.CustomSelectCardsIntermediateResponse), (byte)PacketType.DuelCustomSelectCardsIntermediateResponse},
		{typeof(Structs.NetworkingStructs.DuelPackets.FieldUpdateRequest), (byte)PacketType.DuelFieldUpdateRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.GetOptionsRequest), (byte)PacketType.DuelGetOptionsRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.GetOptionsResponse), (byte)PacketType.DuelGetOptionsResponse},
		{typeof(Structs.NetworkingStructs.DuelPackets.SelectOptionRequest), (byte)PacketType.DuelSelectOptionRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.SelectZoneRequest), (byte)PacketType.DuelSelectZoneRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.SelectZoneResponse), (byte)PacketType.DuelSelectZoneResponse},
		{typeof(Structs.NetworkingStructs.DuelPackets.PassRequest), (byte)PacketType.DuelPassRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.GameResultResponse), (byte)PacketType.DuelGameResultResponse},
		{typeof(Structs.NetworkingStructs.DuelPackets.ViewGraveRequest), (byte)PacketType.DuelViewGraveRequest},
		{typeof(Structs.NetworkingStructs.DuelPackets.ViewCardsResponse), (byte)PacketType.DuelViewCardsResponse},

		{typeof(Structs.NetworkingStructs.ServerPackets.AdditionalCardsRequest), (byte)PacketType.ServerAdditionalCardsRequest},
		{typeof(Structs.NetworkingStructs.ServerPackets.AdditionalCardsResponse), (byte)PacketType.ServerAdditionalCardsResponse},
		{typeof(Structs.NetworkingStructs.ServerPackets.CreateRequest), (byte)PacketType.ServerCreateRequest},
		{typeof(Structs.NetworkingStructs.ServerPackets.CreateResponse), (byte)PacketType.ServerCreateResponse},
		{typeof(Structs.NetworkingStructs.ServerPackets.JoinRequest), (byte)PacketType.ServerJoinRequest},
		{typeof(Structs.NetworkingStructs.ServerPackets.JoinResponse), (byte)PacketType.ServerJoinResponse},
		{typeof(Structs.NetworkingStructs.ServerPackets.OpponentChangedResponse), (byte)PacketType.ServerOpponentChangedResponse},
		{typeof(Structs.NetworkingStructs.ServerPackets.LeaveRequest), (byte)PacketType.ServerLeaveRequest},
		{typeof(Structs.NetworkingStructs.ServerPackets.RoomsRequest), (byte)PacketType.ServerRoomsRequest},
		{typeof(Structs.NetworkingStructs.ServerPackets.RoomsResponse), (byte)PacketType.ServerRoomsResponse},
		{typeof(Structs.NetworkingStructs.ServerPackets.StartRequest), (byte)PacketType.ServerStartRequest},
		{typeof(Structs.NetworkingStructs.ServerPackets.StartResponse), (byte)PacketType.ServerStartResponse},
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
