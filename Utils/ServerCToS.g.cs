using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Structs.Server;

#nullable enable

public record CToS_Packet(CToS_Content content) : Common.PacketTable
{
	public byte[] Deserialize()
	{
		List<byte> dataBytes = DeserializeInternal();
		return [.. Common.Common.DeserializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.DeserializeN16(2) /* ProtoVersion */,
			.. Common.Common.DeserializeN16(1) /* SchemaVersion */,
			.. Common.Common.DeserializeName("CToS_Packet") /* Name */,
			.. dataBytes /* Root */];
	}

	public static CToS_Packet Serialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.SerializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return SerializeImpl(ref bytes);
	}
	public static CToS_Packet Serialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.SerializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return SerializeImpl(ref bytes);
	}
	private static CToS_Packet SerializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.SerializeName(ref bytes, "CToS_Packet"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		CToS_Packet ret = SerializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static CToS_Packet SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header content */
		{
			if(!Common.Common.SerializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header CToS_Packet.content hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for CToS_Packet.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		CToS_Content content = CToS_Content.SerializeInternal(ref bytes);
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
public interface CToS_Content : Common.PacketUnion
{
	public static CToS_Content SerializeInternal(ref Span<byte> bytes)
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
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("leave")))
		{
			return leave.SerializeInternal(ref bytes);
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
			throw new Exception("Unknown union variant in CToS_Content");
		}
	}

	public record additional_cards() : CToS_Content
	{
		public static additional_cards SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/additional_cards, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> DeserializeInternal()
		{
			return [.. Common.Common.DeserializeName("additional_cards"), (byte)Common.TypeBytes.Void];
		}
	}
	public record artworks(CToS_Request_Artworks value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/artworks, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Artworks value = CToS_Request_Artworks.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record create(CToS_Request_Create value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/create, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Create value = CToS_Request_Create.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record join(CToS_Request_Join value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/join, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Join value = CToS_Request_Join.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record leave() : CToS_Content
	{
		public static leave SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/leave, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> DeserializeInternal()
		{
			return [.. Common.Common.DeserializeName("leave"), (byte)Common.TypeBytes.Void];
		}
	}
	public record rooms() : CToS_Content
	{
		public static rooms SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/rooms, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> DeserializeInternal()
		{
			return [.. Common.Common.DeserializeName("rooms"), (byte)Common.TypeBytes.Void];
		}
	}
	public record start(CToS_Request_Start value) : CToS_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("start")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static start SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/start, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Start value = CToS_Request_Start.SerializeInternal(ref bytes);
			return new(value);
		}
	}
}
public record CToS_Request_Artworks(List<string> names) : Common.PacketTable
{
	public static CToS_Request_Artworks SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header names */
		{
			if(!Common.Common.SerializeName(ref bytes, "names")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Artworks.names hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for CToS_Request_Artworks.names, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
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
public record CToS_Request_Create(string name) : Common.PacketTable
{
	public static CToS_Request_Create SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.SerializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Create.name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Create.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		string name = Common.Common.SerializeStr(ref bytes);
		return new(name);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.DeserializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.DeserializeStr(name));
		return bytes;
	}
}
public record CToS_Request_Join(string own_name, string opp_name) : Common.PacketTable
{
	public static CToS_Request_Join SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header own_name */
		{
			if(!Common.Common.SerializeName(ref bytes, "own_name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Join.own_name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Join.own_name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header opp_name */
		{
			if(!Common.Common.SerializeName(ref bytes, "opp_name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Join.opp_name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Join.opp_name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		string own_name = Common.Common.SerializeStr(ref bytes);
		string opp_name = Common.Common.SerializeStr(ref bytes);
		return new(own_name, opp_name);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header own_name */
		bytes.AddRange(Common.Common.DeserializeName("own_name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header opp_name */
		bytes.AddRange(Common.Common.DeserializeName("opp_name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data own_name */
		bytes.AddRange(Common.Common.DeserializeStr(own_name));
		/* Data opp_name */
		bytes.AddRange(Common.Common.DeserializeStr(opp_name));
		return bytes;
	}
}
public record CToS_Request_Start(CardGameUtils.Base.Deck decklist, bool no_shuffle) : Common.PacketTable
{
	public static CToS_Request_Start SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header decklist */
		{
			if(!Common.Common.SerializeName(ref bytes, "decklist")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Start.decklist hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Request_Start.decklist, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		/* Field Header no_shuffle */
		{
			if(!Common.Common.SerializeName(ref bytes, "no_shuffle")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Start.no_shuffle hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for CToS_Request_Start.no_shuffle, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		CardGameUtils.Base.Deck decklist = CardGameUtils.Base.Deck.SerializeInternal(ref bytes);
		bool no_shuffle = Common.Common.SerializeBool(ref bytes);
		return new(decklist, no_shuffle);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header decklist */
		bytes.AddRange(Common.Common.DeserializeName("decklist")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Field Header no_shuffle */
		bytes.AddRange(Common.Common.DeserializeName("no_shuffle")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data decklist */
		bytes.AddRange(decklist.DeserializeInternal());
		/* Data no_shuffle */
		bytes.AddRange(Common.Common.DeserializeBool(no_shuffle));
		return bytes;
	}
}
