using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CardGameUtils.Structs.Server;

#nullable enable
#pragma warning disable CS8981
internal record SToC_Packet(SToC_Content content) : Common.PacketTable
{
	public byte[] Serialize()
	{
		List<byte> dataBytes = SerializeInternal();
		return [.. Common.Common.SerializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.SerializeN16(2) /* ProtoVersion */,
			.. Common.Common.SerializeN16(1) /* SchemaVersion */,
			.. Common.Common.SerializeName("SToC_Packet") /* Name */,
			.. dataBytes /* Root */];
	}

	public static SToC_Packet Deserialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.DeserializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return DeserializeImpl(ref bytes);
	}
	public static SToC_Packet Deserialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return DeserializeImpl(ref bytes);
	}
	public static async Task<SToC_Packet> DeserializeAsync(Stream stream, CancellationToken token)
	{
		byte[] sizeBytes = new byte[4];
		await stream.ReadExactlyAsync(sizeBytes.AsMemory(), token);
		Span<byte> sizeSpan = sizeBytes.AsSpan();
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		byte[] bytes = new byte[size];
		await stream.ReadExactlyAsync(bytes.AsMemory(), token);
		Span<byte> byteSpan = bytes.AsSpan();
		return DeserializeImpl(ref byteSpan);
	}
	private static SToC_Packet DeserializeImpl(ref Span<byte> bytes)
	{
		ushort protoVersion = Common.Common.DeserializeN16(ref bytes);
		if(protoVersion != 2)
		{
			throw new Exception($"Wrong proto version, expected 2, got {protoVersion}");
		}
		ushort schemaVersion = Common.Common.DeserializeN16(ref bytes);
		if(schemaVersion != 1)
		{
			throw new Exception($"Wrong schema version, expected 1, got {schemaVersion}");
		}
		if(!Common.Common.DeserializeName(ref bytes, "SToC_Packet"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		SToC_Packet ret = DeserializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static SToC_Packet DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header content */
		{
			if(!Common.Common.DeserializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header SToC_Packet.content hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Packet.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		SToC_Content content = SToC_Content.DeserializeInternal(ref bytes);
		return new(content);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header content */
		bytes.AddRange(Common.Common.SerializeName("content")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data content */
		bytes.AddRange(content.SerializeInternal());
		return bytes;
	}
}
internal interface SToC_Content : Common.PacketUnion
{
	public static SToC_Content DeserializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.SerializeName("additional_cards")))
		{
			return additional_cards.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("artworks")))
		{
			return artworks.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("create")))
		{
			return create.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("join")))
		{
			return join.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("opponent_changed")))
		{
			return opponent_changed.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("rooms")))
		{
			return rooms.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("start")))
		{
			return start.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in SToC_Content");
		}
	}

	internal record additional_cards(SToC_Response_AdditionalCards value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("additional_cards")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static additional_cards DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/additional_cards, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_AdditionalCards value = SToC_Response_AdditionalCards.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record artworks(SToC_Response_Artworks value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("artworks")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static artworks DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/artworks, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Artworks value = SToC_Response_Artworks.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record create(SToC_Response_Create value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("create")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static create DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/create, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Create value = SToC_Response_Create.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record join(SToC_Response_Join value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("join")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static join DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/join, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Join value = SToC_Response_Join.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record opponent_changed(SToC_Broadcast_OpponentChanged value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("opponent_changed")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static opponent_changed DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/opponent_changed, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Broadcast_OpponentChanged value = SToC_Broadcast_OpponentChanged.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record rooms(SToC_Response_Rooms value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("rooms")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static rooms DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/rooms, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Rooms value = SToC_Response_Rooms.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record start(SToC_Response_Start value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("start")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static start DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Content/start, expected `{(byte)(Common.TypeBytes.Union)}`, got `{type}`");
			}
			SToC_Response_Start value = SToC_Response_Start.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
}
internal record SToC_Response_AdditionalCards(ulong timestamp, List<CardGameUtils.Base.CardStruct> cards) : Common.PacketTable
{
	public static SToC_Response_AdditionalCards DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header timestamp */
		{
			if(!Common.Common.DeserializeName(ref bytes, "timestamp")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_AdditionalCards.timestamp hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N64))
			{
				throw new Exception($"Wrong field type for SToC_Response_AdditionalCards.timestamp, expected {(byte)(Common.TypeBytes.N64)}, got {type}");
			}
		}
		/* Field Header cards */
		{
			if(!Common.Common.DeserializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_AdditionalCards.cards hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_AdditionalCards.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		ulong timestamp = Common.Common.DeserializeN64(ref bytes);
		byte cardsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(cardsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cards, expected 0, got {cardsNestingLevel}");
		}
		uint cardsCount = Common.Common.DeserializeN32(ref bytes);
		List<CardGameUtils.Base.CardStruct> cards = new((int)cardsCount);
		for(int cards_ = 0; cards_ < cards.Capacity; cards_++)
		{
			cards.Add(CardGameUtils.Base.CardStruct.DeserializeInternal(ref bytes));
		}
		return new(timestamp, cards);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header timestamp */
		bytes.AddRange(Common.Common.SerializeName("timestamp")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N64)); /* Type */
		/* Field Header cards */
		bytes.AddRange(Common.Common.SerializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data timestamp */
		bytes.AddRange(Common.Common.SerializeN64(timestamp));
		/* Data cards */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)cards.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cards_ in cards)
		{
			bytes.AddRange(cards_.SerializeInternal());
		}
		return bytes;
	}
}
internal record SToC_Response_Artworks(List<Artwork> artworks) : Common.PacketTable
{
	public static SToC_Response_Artworks DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header artworks */
		{
			if(!Common.Common.DeserializeName(ref bytes, "artworks")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Artworks.artworks hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Artworks.artworks, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte artworksNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(artworksNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for artworks, expected 0, got {artworksNestingLevel}");
		}
		uint artworksCount = Common.Common.DeserializeN32(ref bytes);
		List<Artwork> artworks = new((int)artworksCount);
		for(int artworks_ = 0; artworks_ < artworks.Capacity; artworks_++)
		{
			artworks.Add(Artwork.DeserializeInternal(ref bytes));
		}
		return new(artworks);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header artworks */
		bytes.AddRange(Common.Common.SerializeName("artworks")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data artworks */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)artworks.Count)); /* Count */
		/* Nesting Counts */
		foreach(var artworks_ in artworks)
		{
			bytes.AddRange(artworks_.SerializeInternal());
		}
		return bytes;
	}
}
internal record Artwork(string name, ArtworkFiletype filetype, List<byte> data) : Common.PacketTable
{
	public static Artwork DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header Artwork.name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for Artwork.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header filetype */
		{
			if(!Common.Common.DeserializeName(ref bytes, "filetype")) /* Name */
			{
				throw new Exception("Field Header Artwork.filetype hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for Artwork.filetype, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header data */
		{
			if(!Common.Common.DeserializeName(ref bytes, "data")) /* Name */
			{
				throw new Exception("Field Header Artwork.data hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N8 | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Artwork.data, expected {(byte)(Common.TypeBytes.N8 | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		string name = Common.Common.DeserializeStr(ref bytes);
		ArtworkFiletype filetype = (ArtworkFiletype)Common.Common.DeserializeN8(ref bytes);
		if(!Common.Common.DeserializeName(ref bytes, Enum.GetName(filetype)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.SerializeName(Enum.GetName(filetype)!))}]");
		}
		byte dataNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(dataNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for data, expected 0, got {dataNestingLevel}");
		}
		uint dataCount = Common.Common.DeserializeN32(ref bytes);
		List<byte> data = new((int)dataCount);
		for(int data_ = 0; data_ < data.Capacity; data_++)
		{
			data.Add(Common.Common.DeserializeN8(ref bytes));
		}
		return new(name, filetype, data);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.SerializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header filetype */
		bytes.AddRange(Common.Common.SerializeName("filetype")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header data */
		bytes.AddRange(Common.Common.SerializeName("data")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N8 | Common.TypeBytes.ListFlag)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.SerializeStr(name));
		/* Data filetype */
		bytes.AddRange(Common.Common.SerializeN8((byte)filetype));
		bytes.AddRange(Common.Common.SerializeName(Enum.GetName(filetype)!, len: 3));
		/* Data data */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)data.Count)); /* Count */
		/* Nesting Counts */
		foreach(var data_ in data)
		{
			bytes.AddRange(Common.Common.SerializeN8(data_));
		}
		return bytes;
	}
}
internal enum ArtworkFiletype
{
	JPG,
	PNG,
}
internal record SToC_Response_Create(CardGameUtils.Base.ErrorOr success) : Common.PacketTable
{
	public static SToC_Response_Create DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header success */
		{
			if(!Common.Common.DeserializeName(ref bytes, "success")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Create.success hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Response_Create.success, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		CardGameUtils.Base.ErrorOr success = CardGameUtils.Base.ErrorOr.DeserializeInternal(ref bytes);
		return new(success);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header success */
		bytes.AddRange(Common.Common.SerializeName("success")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data success */
		bytes.AddRange(success.SerializeInternal());
		return bytes;
	}
}
internal record SToC_Response_Join(CardGameUtils.Base.ErrorOr success) : Common.PacketTable
{
	public static SToC_Response_Join DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header success */
		{
			if(!Common.Common.DeserializeName(ref bytes, "success")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Join.success hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Response_Join.success, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		CardGameUtils.Base.ErrorOr success = CardGameUtils.Base.ErrorOr.DeserializeInternal(ref bytes);
		return new(success);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header success */
		bytes.AddRange(Common.Common.SerializeName("success")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data success */
		bytes.AddRange(success.SerializeInternal());
		return bytes;
	}
}
internal record SToC_Broadcast_OpponentChanged(string? name) : Common.PacketTable
{
	public static SToC_Broadcast_OpponentChanged DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_OpponentChanged.name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_OpponentChanged.name, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		string? name = null;
		if(Common.Common.DeserializeBool(ref bytes))
		{
			name = Common.Common.DeserializeStr(ref bytes);
		}
		return new(name);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.SerializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data name */
		if(name is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(Common.Common.SerializeStr(name));
		}
		return bytes;
	}
}
internal record SToC_Response_Rooms(List<string> rooms) : Common.PacketTable
{
	public static SToC_Response_Rooms DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header rooms */
		{
			if(!Common.Common.DeserializeName(ref bytes, "rooms")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Rooms.rooms hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Rooms.rooms, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte roomsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(roomsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for rooms, expected 0, got {roomsNestingLevel}");
		}
		uint roomsCount = Common.Common.DeserializeN32(ref bytes);
		List<string> rooms = new((int)roomsCount);
		for(int rooms_ = 0; rooms_ < rooms.Capacity; rooms_++)
		{
			rooms.Add(Common.Common.DeserializeStr(ref bytes));
		}
		return new(rooms);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header rooms */
		bytes.AddRange(Common.Common.SerializeName("rooms")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)); /* Type */
		/* Data rooms */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)rooms.Count)); /* Count */
		/* Nesting Counts */
		foreach(var rooms_ in rooms)
		{
			bytes.AddRange(Common.Common.SerializeStr(rooms_));
		}
		return bytes;
	}
}
internal interface SToC_Response_Start : Common.PacketUnion
{
	public static SToC_Response_Start DeserializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.SerializeName("failure")))
		{
			return failure.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("success")))
		{
			return success.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("success_but_waiting")))
		{
			return success_but_waiting.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in SToC_Response_Start");
		}
	}

	internal record failure(string value) : SToC_Response_Start
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("failure")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
			bytes.AddRange(Common.Common.SerializeStr(value));
			return bytes;
		}

		public static failure DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for SToC_Response_Start/failure, expected `{(byte)(Common.TypeBytes.Str)}`, got `{type}`");
			}
			string value = Common.Common.DeserializeStr(ref bytes);
			return new(value);
		}
	}
	internal record success(Success value) : SToC_Response_Start
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("success")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static success DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Response_Start/success, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			Success value = Success.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record success_but_waiting() : SToC_Response_Start
	{
		public static success_but_waiting DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for SToC_Response_Start/success_but_waiting, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("success_but_waiting"), (byte)Common.TypeBytes.Void];
		}
	}
}
internal record Success(string id, int port) : Common.PacketTable
{
	public static Success DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header id */
		{
			if(!Common.Common.DeserializeName(ref bytes, "id")) /* Name */
			{
				throw new Exception("Field Header Success.id hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for Success.id, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header port */
		{
			if(!Common.Common.DeserializeName(ref bytes, "port")) /* Name */
			{
				throw new Exception("Field Header Success.port hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for Success.port, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		string id = Common.Common.DeserializeStr(ref bytes);
		int port = Common.Common.DeserializeI32(ref bytes);
		return new(id, port);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header id */
		bytes.AddRange(Common.Common.SerializeName("id")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header port */
		bytes.AddRange(Common.Common.SerializeName("port")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Data id */
		bytes.AddRange(Common.Common.SerializeStr(id));
		/* Data port */
		bytes.AddRange(Common.Common.SerializeI32(port));
		return bytes;
	}
}
