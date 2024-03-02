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
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.ListRequest))]
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.ListResponse))]
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.ListUpdateRequest))]
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.ListUpdateResponse))]
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.NamesRequest))]
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.NamesResponse))]
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.SearchRequest))]
[JsonSerializable(typeof(NetworkingStructs.DeckPackets.SearchResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.CustomSelectCardsIntermediateRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.CustomSelectCardsIntermediateResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.CustomSelectCardsRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.CustomSelectCardsResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.FieldUpdateRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.FieldUpdateRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.GameResultResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.GetOptionsRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.GetOptionsResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.GetOptionsResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.PassRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.SelectCardsRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.SelectCardsResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.SelectOptionRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.SelectZoneRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.SelectZoneResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.SurrenderRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.ViewCardsResponse))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.ViewGraveRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.YesNoRequest))]
[JsonSerializable(typeof(NetworkingStructs.DuelPackets.YesNoResponse))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.AdditionalCardsRequest))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.AdditionalCardsResponse))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.CreateRequest))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.CreateResponse))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.JoinRequest))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.JoinResponse))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.LeaveRequest))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.OpponentChangedResponse))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.RoomsRequest))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.RoomsResponse))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.StartRequest))]
[JsonSerializable(typeof(NetworkingStructs.ServerPackets.StartResponse))]
internal partial class PacketSerializationContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(Replay))]
internal partial class ReplaySerializationContext : JsonSerializerContext { }
