using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Structs.Deck;

#nullable enable
#pragma warning disable CS8981
public record CToS_Packet(CToS_Content content) : Common.PacketTable
{
	public byte[] Serialize()
	{
		List<byte> dataBytes = SerializeInternal();
		return [.. Common.Common.SerializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.SerializeN16(2) /* ProtoVersion */,
			.. Common.Common.SerializeN16(1) /* SchemaVersion */,
			.. Common.Common.SerializeName("CToS_Packet") /* Name */,
			.. dataBytes /* Root */];
	}

	public static CToS_Packet Deserialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.DeserializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return DeserializeImpl(ref bytes);
	}
	public static CToS_Packet Deserialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return DeserializeImpl(ref bytes);
	}
	private static CToS_Packet DeserializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.DeserializeName(ref bytes, "CToS_Packet"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		CToS_Packet ret = DeserializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static CToS_Packet DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header content */
		{
			if(!Common.Common.DeserializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header CToS_Packet.content hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for CToS_Packet.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		CToS_Content content = CToS_Content.DeserializeInternal(ref bytes);
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
public interface CToS_Content : Common.PacketUnion
{
	public static CToS_Content DeserializeInternal(ref Span<byte> bytes)
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
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("decklist_update")))
		{
			return decklist_update.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("decklist_delete")))
		{
			return decklist_delete.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("search")))
		{
			return search.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in CToS_Content");
		}
	}

	public record decklists() : CToS_Content
	{
		public static decklists DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/decklists, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("decklists"), (byte)Common.TypeBytes.Void];
		}
	}
	public record decklist(CToS_Request_Decklist value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/decklist, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Decklist value = CToS_Request_Decklist.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record decklist_update(CToS_Request_DecklistUpdate value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("decklist_update")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static decklist_update DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/decklist_update, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_DecklistUpdate value = CToS_Request_DecklistUpdate.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record decklist_delete(CToS_Request_DecklistDelete value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("decklist_delete")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static decklist_delete DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/decklist_delete, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_DecklistDelete value = CToS_Request_DecklistDelete.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record search(CToS_Request_Search value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/search, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Search value = CToS_Request_Search.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
}
public record CToS_Request_Decklist(string name) : Common.PacketTable
{
	public static CToS_Request_Decklist DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Decklist.name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Decklist.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		string name = Common.Common.DeserializeStr(ref bytes);
		return new(name);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.SerializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.SerializeStr(name));
		return bytes;
	}
}
public record CToS_Request_DecklistUpdate(CardGameUtils.Base.Deck deck) : Common.PacketTable
{
	public static CToS_Request_DecklistUpdate DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header deck */
		{
			if(!Common.Common.DeserializeName(ref bytes, "deck")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_DecklistUpdate.deck hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Request_DecklistUpdate.deck, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		CardGameUtils.Base.Deck deck = CardGameUtils.Base.Deck.DeserializeInternal(ref bytes);
		return new(deck);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header deck */
		bytes.AddRange(Common.Common.SerializeName("deck")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Data deck */
		bytes.AddRange(deck.SerializeInternal());
		return bytes;
	}
}
public record CToS_Request_DecklistDelete(string name) : Common.PacketTable
{
	public static CToS_Request_DecklistDelete DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_DecklistDelete.name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_DecklistDelete.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		string name = Common.Common.DeserializeStr(ref bytes);
		return new(name);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.SerializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.SerializeStr(name));
		return bytes;
	}
}
public record CToS_Request_Search(string filter, CardGameUtils.GameConstants.PlayerClass player_class, bool include_generic_cards) : Common.PacketTable
{
	public static CToS_Request_Search DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header filter */
		{
			if(!Common.Common.DeserializeName(ref bytes, "filter")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Search.filter hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Search.filter, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header player_class */
		{
			if(!Common.Common.DeserializeName(ref bytes, "player_class")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Search.player_class hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for CToS_Request_Search.player_class, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header include_generic_cards */
		{
			if(!Common.Common.DeserializeName(ref bytes, "include_generic_cards")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Search.include_generic_cards hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for CToS_Request_Search.include_generic_cards, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		string filter = Common.Common.DeserializeStr(ref bytes);
		CardGameUtils.GameConstants.PlayerClass player_class = (CardGameUtils.GameConstants.PlayerClass)Common.Common.DeserializeN8(ref bytes);
		if(!Common.Common.DeserializeName(ref bytes, Enum.GetName(player_class)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.SerializeName(Enum.GetName(player_class)!))}]");
		}
		bool include_generic_cards = Common.Common.DeserializeBool(ref bytes);
		return new(filter, player_class, include_generic_cards);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header filter */
		bytes.AddRange(Common.Common.SerializeName("filter")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header player_class */
		bytes.AddRange(Common.Common.SerializeName("player_class")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header include_generic_cards */
		bytes.AddRange(Common.Common.SerializeName("include_generic_cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data filter */
		bytes.AddRange(Common.Common.SerializeStr(filter));
		/* Data player_class */
		bytes.AddRange(Common.Common.SerializeN8((byte)player_class));
		bytes.AddRange(Common.Common.SerializeName(Enum.GetName(player_class)!, len: 3));
		/* Data include_generic_cards */
		bytes.AddRange(Common.Common.SerializeBool(include_generic_cards));
		return bytes;
	}
}
