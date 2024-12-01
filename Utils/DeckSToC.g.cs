using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Structs.Deck;

#nullable enable
#pragma warning disable CS8981
public record SToC_Packet(SToC_Content content) : Common.PacketTable
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
public interface SToC_Content : Common.PacketUnion
{
	public static SToC_Content DeserializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.SerializeName("decklists")))
		{
			return decklists.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("decklist")))
		{
			return decklist.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("search")))
		{
			return search.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in SToC_Content");
		}
	}

	public record decklists(SToC_Response_Decklists value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("decklists")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static decklists DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/decklists, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Decklists value = SToC_Response_Decklists.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record decklist(SToC_Response_Decklist value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("decklist")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static decklist DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/decklist, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Decklist value = SToC_Response_Decklist.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record search(SToC_Response_Search value) : SToC_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("search")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static search DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/search, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Search value = SToC_Response_Search.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
}
public record SToC_Response_Decklists(List<string> names) : Common.PacketTable
{
	public static SToC_Response_Decklists DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header names */
		{
			if(!Common.Common.DeserializeName(ref bytes, "names")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Decklists.names hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Decklists.names, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte namesNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(namesNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for names, expected 0, got {namesNestingLevel}");
		}
		uint namesCount = Common.Common.DeserializeN32(ref bytes);
		List<string> names = new((int)namesCount);
		for(int names_ = 0; names_ < names.Capacity; names_++)
		{
			names.Add(Common.Common.DeserializeStr(ref bytes));
		}
		return new(names);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header names */
		bytes.AddRange(Common.Common.SerializeName("names")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)); /* Type */
		/* Data names */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)names.Count)); /* Count */
		/* Nesting Counts */
		foreach(var names_ in names)
		{
			bytes.AddRange(Common.Common.SerializeStr(names_));
		}
		return bytes;
	}
}
public record SToC_Response_Decklist(CardGameUtils.Base.Deck? deck) : Common.PacketTable
{
	public static SToC_Response_Decklist DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header deck */
		{
			if(!Common.Common.DeserializeName(ref bytes, "deck")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Decklist.deck hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Decklist.deck, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		CardGameUtils.Base.Deck? deck = null;
		if(Common.Common.DeserializeBool(ref bytes))
		{
			deck = CardGameUtils.Base.Deck.DeserializeInternal(ref bytes);
		}
		return new(deck);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header deck */
		bytes.AddRange(Common.Common.SerializeName("deck")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data deck */
		if(deck is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(deck.SerializeInternal());
		}
		return bytes;
	}
}
public record SToC_Response_Search(List<CardGameUtils.Base.CardStruct> cards) : Common.PacketTable
{
	public static SToC_Response_Search DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header cards */
		{
			if(!Common.Common.DeserializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Search.cards hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Search.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
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
		return new(cards);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header cards */
		bytes.AddRange(Common.Common.SerializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
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
