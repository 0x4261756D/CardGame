using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Structs.Server;

#nullable enable

public record SToC_Packet(SToC_Content content) : Common.PacketTable
{
	public byte[] Deserialize()
	{
		List<byte> dataBytes = DeserializeInternal();
		return [.. Common.Common.DeserializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.DeserializeN16(2) /* ProtoVersion */,
			.. Common.Common.DeserializeN16(1) /* SchemaVersion */,
			.. Common.Common.DeserializeName("SToC_Packet") /* Name */,
			.. dataBytes /* Root */];
	}

	public static SToC_Packet Serialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.SerializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return SerializeImpl(ref bytes);
	}
	public static SToC_Packet Serialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.SerializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return SerializeImpl(ref bytes);
	}
	private static SToC_Packet SerializeImpl(ref Span<byte> bytes)
	{
		ushort protoVersion = Common.Common.SerializeN16(ref bytes);
		if(protoVersion != 2)
		{
			throw new Exception($"Wrong proto version, expected 2, got {protoVersion}");
		}
		ushort schemaVersion = Common.Common.SerializeN16(ref bytes);
		if(schemaVersion != 1)
		{
			throw new Exception($"Wrong schema version, expected 1, got {schemaVersion}");
		}
		if(!Common.Common.SerializeName(ref bytes, "SToC_Packet"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		SToC_Packet ret = SerializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static SToC_Packet SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header content */
		{
			if(!Common.Common.SerializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header SToC_Packet.content hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Packet.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		SToC_Content content = SToC_Content.SerializeInternal(ref bytes);
		return new(content);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header content */
		bytes.AddRange(Common.Common.DeserializeName("content")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data content */
		bytes.AddRange(content.DeserializeInternal());
		return bytes;
	}
}
public interface SToC_Content : Common.PacketUnion
{
	public static SToC_Content SerializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.DeserializeName("additional_cards")))
		{
			return additional_cards.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("artworks")))
		{
			return artworks.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("create")))
		{
			return create.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("join")))
		{
			return join.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("opponent_changed")))
		{
			return opponent_changed.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("rooms")))
		{
			return rooms.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("start")))
		{
			return start.SerializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in SToC_Content");
		}
	}

	public record additional_cards(SToC_Response_AdditionalCards value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("additional_cards")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static additional_cards SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/additional_cards, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_AdditionalCards value = SToC_Response_AdditionalCards.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record artworks(SToC_Response_Artworks value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("artworks")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static artworks SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/artworks, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Artworks value = SToC_Response_Artworks.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record create(SToC_Response_Create value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("create")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static create SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/create, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Create value = SToC_Response_Create.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record join(SToC_Response_Join value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("join")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static join SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/join, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Join value = SToC_Response_Join.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record opponent_changed(SToC_Broadcast_OpponentChanged value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("opponent_changed")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static opponent_changed SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/opponent_changed, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Broadcast_OpponentChanged value = SToC_Broadcast_OpponentChanged.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record rooms(SToC_Response_Rooms value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("rooms")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static rooms SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/rooms, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Rooms value = SToC_Response_Rooms.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record start(SToC_Response_Start value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("start")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static start SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Content/start, expected `{(byte)(Common.TypeBytes.Union)}`, got `{type}`");
			}
			SToC_Response_Start value = SToC_Response_Start.SerializeInternal(ref bytes);
			return new(value);
		}
	}
}
public record SToC_Response_AdditionalCards(ulong timestamp, List<CardGameUtils.Base.CardStruct> cards) : Common.PacketTable
{
	public static SToC_Response_AdditionalCards SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header timestamp */
		{
			if(!Common.Common.SerializeName(ref bytes, "timestamp")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_AdditionalCards.timestamp hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N64))
			{
				throw new Exception($"Wrong field type for SToC_Response_AdditionalCards.timestamp, expected {(byte)(Common.TypeBytes.N64)}, got {type}");
			}
		}
		/* Field Header cards */
		{
			if(!Common.Common.SerializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_AdditionalCards.cards hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_AdditionalCards.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		ulong timestamp = Common.Common.SerializeN64(ref bytes);
		byte cardsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(cardsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cards, expected 0, got {cardsNestingLevel}");
		}
		uint cardsCount = Common.Common.SerializeN32(ref bytes);
		List<CardGameUtils.Base.CardStruct> cards = new((int)cardsCount);
		for(int cards_ = 0; cards_ < cards.Capacity; cards_++)
		{
			cards.Add(CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes));
		}
		return new(timestamp, cards);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header timestamp */
		bytes.AddRange(Common.Common.DeserializeName("timestamp")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N64)); /* Type */
		/* Field Header cards */
		bytes.AddRange(Common.Common.DeserializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data timestamp */
		bytes.AddRange(Common.Common.DeserializeN64(timestamp));
		/* Data cards */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)cards.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cards_ in cards)
		{
			bytes.AddRange(cards_.DeserializeInternal());
		}
		return bytes;
	}
}
public record SToC_Response_Artworks(List<Artwork> artworks) : Common.PacketTable
{
	public static SToC_Response_Artworks SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header artworks */
		{
			if(!Common.Common.SerializeName(ref bytes, "artworks")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Artworks.artworks hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Artworks.artworks, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte artworksNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(artworksNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for artworks, expected 0, got {artworksNestingLevel}");
		}
		uint artworksCount = Common.Common.SerializeN32(ref bytes);
		List<Artwork> artworks = new((int)artworksCount);
		for(int artworks_ = 0; artworks_ < artworks.Capacity; artworks_++)
		{
			artworks.Add(Artwork.SerializeInternal(ref bytes));
		}
		return new(artworks);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header artworks */
		bytes.AddRange(Common.Common.DeserializeName("artworks")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data artworks */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)artworks.Count)); /* Count */
		/* Nesting Counts */
		foreach(var artworks_ in artworks)
		{
			bytes.AddRange(artworks_.DeserializeInternal());
		}
		return bytes;
	}
}
public record Artwork(string name, ArtworkFiletype filetype, List<byte> data) : Common.PacketTable
{
	public static Artwork SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.SerializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header Artwork.name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for Artwork.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header filetype */
		{
			if(!Common.Common.SerializeName(ref bytes, "filetype")) /* Name */
			{
				throw new Exception("Field Header Artwork.filetype hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for Artwork.filetype, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header data */
		{
			if(!Common.Common.SerializeName(ref bytes, "data")) /* Name */
			{
				throw new Exception("Field Header Artwork.data hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N8 | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Artwork.data, expected {(byte)(Common.TypeBytes.N8 | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		string name = Common.Common.SerializeStr(ref bytes);
		ArtworkFiletype filetype = (ArtworkFiletype)Common.Common.SerializeN8(ref bytes);
		if(!Common.Common.SerializeName(ref bytes, Enum.GetName(filetype)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.DeserializeName(Enum.GetName(filetype)!))}]");
		}
		byte dataNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(dataNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for data, expected 0, got {dataNestingLevel}");
		}
		uint dataCount = Common.Common.SerializeN32(ref bytes);
		List<byte> data = new((int)dataCount);
		for(int data_ = 0; data_ < data.Capacity; data_++)
		{
			data.Add(Common.Common.SerializeN8(ref bytes));
		}
		return new(name, filetype, data);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.DeserializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header filetype */
		bytes.AddRange(Common.Common.DeserializeName("filetype")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header data */
		bytes.AddRange(Common.Common.DeserializeName("data")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N8 | Common.TypeBytes.ListFlag)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.DeserializeStr(name));
		/* Data filetype */
		bytes.AddRange(Common.Common.DeserializeN8((byte)filetype));
		bytes.AddRange(Common.Common.DeserializeName(Enum.GetName(filetype)!, len: 3));
		/* Data data */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)data.Count)); /* Count */
		/* Nesting Counts */
		foreach(var data_ in data)
		{
			bytes.AddRange(Common.Common.DeserializeN8(data_));
		}
		return bytes;
	}
}
public enum ArtworkFiletype
{
	JPG,
	PNG,
}
public record SToC_Response_Create(CardGameUtils.Base.ErrorOr success) : Common.PacketTable
{
	public static SToC_Response_Create SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header success */
		{
			if(!Common.Common.SerializeName(ref bytes, "success")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Create.success hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Response_Create.success, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		CardGameUtils.Base.ErrorOr success = CardGameUtils.Base.ErrorOr.SerializeInternal(ref bytes);
		return new(success);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header success */
		bytes.AddRange(Common.Common.DeserializeName("success")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data success */
		bytes.AddRange(success.DeserializeInternal());
		return bytes;
	}
}
public record SToC_Response_Join(CardGameUtils.Base.ErrorOr success) : Common.PacketTable
{
	public static SToC_Response_Join SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header success */
		{
			if(!Common.Common.SerializeName(ref bytes, "success")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Join.success hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Response_Join.success, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		CardGameUtils.Base.ErrorOr success = CardGameUtils.Base.ErrorOr.SerializeInternal(ref bytes);
		return new(success);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header success */
		bytes.AddRange(Common.Common.DeserializeName("success")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data success */
		bytes.AddRange(success.DeserializeInternal());
		return bytes;
	}
}
public record SToC_Broadcast_OpponentChanged(string? name) : Common.PacketTable
{
	public static SToC_Broadcast_OpponentChanged SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.SerializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_OpponentChanged.name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_OpponentChanged.name, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		string? name = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			name = Common.Common.SerializeStr(ref bytes);
		}
		return new(name);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.DeserializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data name */
		if(name is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(Common.Common.DeserializeStr(name));
		}
		return bytes;
	}
}
public record SToC_Response_Rooms(List<string> rooms) : Common.PacketTable
{
	public static SToC_Response_Rooms SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header rooms */
		{
			if(!Common.Common.SerializeName(ref bytes, "rooms")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Rooms.rooms hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Rooms.rooms, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte roomsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(roomsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for rooms, expected 0, got {roomsNestingLevel}");
		}
		uint roomsCount = Common.Common.SerializeN32(ref bytes);
		List<string> rooms = new((int)roomsCount);
		for(int rooms_ = 0; rooms_ < rooms.Capacity; rooms_++)
		{
			rooms.Add(Common.Common.SerializeStr(ref bytes));
		}
		return new(rooms);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header rooms */
		bytes.AddRange(Common.Common.DeserializeName("rooms")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)); /* Type */
		/* Data rooms */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)rooms.Count)); /* Count */
		/* Nesting Counts */
		foreach(var rooms_ in rooms)
		{
			bytes.AddRange(Common.Common.DeserializeStr(rooms_));
		}
		return bytes;
	}
}
public interface SToC_Response_Start : Common.PacketUnion
{
	public static SToC_Response_Start SerializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.DeserializeName("failure")))
		{
			return failure.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("success")))
		{
			return new success();
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("success_but_waiting")))
		{
			return success_but_waiting.SerializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in SToC_Response_Start");
		}
	}

	public record failure(string value) : SToC_Response_Start
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("failure")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
			bytes.AddRange(Common.Common.DeserializeStr(value));
			return bytes;
		}

		public static failure SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for SToC_Response_Start/failure, expected `{(byte)(Common.TypeBytes.Str)}`, got `{type}`");
			}
			string value = Common.Common.SerializeStr(ref bytes);
			return new(value);
		}
	}
	public record success() : SToC_Response_Start
	{
		public static success SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for SToC_Response_Start/success, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> DeserializeInternal()
		{
			return [];
		}
	}
	public record success_but_waiting(SuccessButWaiting value) : SToC_Response_Start
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("success_but_waiting")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static success_but_waiting SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Response_Start/success_but_waiting, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SuccessButWaiting value = SuccessButWaiting.SerializeInternal(ref bytes);
			return new(value);
		}
	}
}
public record SuccessButWaiting(string id, int port) : Common.PacketTable
{
	public static SuccessButWaiting SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header id */
		{
			if(!Common.Common.SerializeName(ref bytes, "id")) /* Name */
			{
				throw new Exception("Field Header SuccessButWaiting.id hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for SuccessButWaiting.id, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header port */
		{
			if(!Common.Common.SerializeName(ref bytes, "port")) /* Name */
			{
				throw new Exception("Field Header SuccessButWaiting.port hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for SuccessButWaiting.port, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		string id = Common.Common.SerializeStr(ref bytes);
		int port = Common.Common.SerializeI32(ref bytes);
		return new(id, port);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header id */
		bytes.AddRange(Common.Common.DeserializeName("id")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header port */
		bytes.AddRange(Common.Common.DeserializeName("port")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Data id */
		bytes.AddRange(Common.Common.DeserializeStr(id));
		/* Data port */
		bytes.AddRange(Common.Common.DeserializeI32(port));
		return bytes;
	}
}
