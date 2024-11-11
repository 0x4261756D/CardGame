using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Structs.Deck;

#nullable enable
#pragma warning disable CS8981

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
		if(nameSpan.SequenceEqual(Common.Common.DeserializeName("decklists")))
		{
			return decklists.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("decklist")))
		{
			return decklist.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("search")))
		{
			return search.SerializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in SToC_Content");
		}
	}

	public record decklists(SToC_Response_Decklists value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("decklists")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static decklists SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/decklists, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Decklists value = SToC_Response_Decklists.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record decklist(SToC_Response_Decklist value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("decklist")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static decklist SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/decklist, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Decklist value = SToC_Response_Decklist.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record search(SToC_Response_Search value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("search")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static search SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/search, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_Search value = SToC_Response_Search.SerializeInternal(ref bytes);
			return new(value);
		}
	}
}
public record SToC_Response_Decklists(List<string> names) : Common.PacketTable
{
	public static SToC_Response_Decklists SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header names */
		{
			if(!Common.Common.SerializeName(ref bytes, "names")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Decklists.names hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Decklists.names, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte namesNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(namesNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for names, expected 0, got {namesNestingLevel}");
		}
		uint namesCount = Common.Common.SerializeN32(ref bytes);
		List<string> names = new((int)namesCount);
		for(int names_ = 0; names_ < names.Capacity; names_++)
		{
			names.Add(Common.Common.SerializeStr(ref bytes));
		}
		return new(names);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header names */
		bytes.AddRange(Common.Common.DeserializeName("names")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)); /* Type */
		/* Data names */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)names.Count)); /* Count */
		/* Nesting Counts */
		foreach(var names_ in names)
		{
			bytes.AddRange(Common.Common.DeserializeStr(names_));
		}
		return bytes;
	}
}
public record SToC_Response_Decklist(CardGameUtils.Base.Deck? deck) : Common.PacketTable
{
	public static SToC_Response_Decklist SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header deck */
		{
			if(!Common.Common.SerializeName(ref bytes, "deck")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Decklist.deck hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Decklist.deck, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		CardGameUtils.Base.Deck? deck = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			deck = CardGameUtils.Base.Deck.SerializeInternal(ref bytes);
		}
		return new(deck);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header deck */
		bytes.AddRange(Common.Common.DeserializeName("deck")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data deck */
		if(deck is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(deck.DeserializeInternal());
		}
		return bytes;
	}
}
public record SToC_Response_Search(List<CardGameUtils.Base.CardStruct> cards) : Common.PacketTable
{
	public static SToC_Response_Search SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header cards */
		{
			if(!Common.Common.SerializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_Search.cards hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_Search.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
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
		return new(cards);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header cards */
		bytes.AddRange(Common.Common.DeserializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
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
