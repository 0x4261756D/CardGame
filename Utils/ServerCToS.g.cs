using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CardGameUtils.Structs.Server;

#nullable enable
#pragma warning disable CS8981
internal record CToS_Packet(CToS_Content content) : Common.PacketTable
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
	public static async Task<CToS_Packet> DeserializeAsync(Stream stream, CancellationToken token)
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
internal interface CToS_Content : Common.PacketUnion
{
	public static CToS_Content DeserializeInternal(ref Span<byte> bytes)
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
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("leave")))
		{
			return leave.DeserializeInternal(ref bytes);
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
			throw new Exception("Unknown union variant in CToS_Content");
		}
	}

	internal record additional_cards() : CToS_Content
	{
		public static additional_cards DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/additional_cards, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("additional_cards"), (byte)Common.TypeBytes.Void];
		}
	}
	internal record artworks(CToS_Request_Artworks value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/artworks, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Artworks value = CToS_Request_Artworks.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record create(CToS_Request_Create value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/create, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Create value = CToS_Request_Create.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record join(CToS_Request_Join value) : CToS_Content
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
				throw new Exception($"Wrong field type for CToS_Content/join, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Join value = CToS_Request_Join.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record leave() : CToS_Content
	{
		public static leave DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/leave, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("leave"), (byte)Common.TypeBytes.Void];
		}
	}
	internal record rooms() : CToS_Content
	{
		public static rooms DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/rooms, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("rooms"), (byte)Common.TypeBytes.Void];
		}
	}
	internal record start(CToS_Request_Start value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("start")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static start DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/start, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_Start value = CToS_Request_Start.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
}
internal record CToS_Request_Artworks(List<string> names) : Common.PacketTable
{
	public static CToS_Request_Artworks DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header names */
		{
			if(!Common.Common.DeserializeName(ref bytes, "names")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Artworks.names hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for CToS_Request_Artworks.names, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
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
internal record CToS_Request_Create(string name) : Common.PacketTable
{
	public static CToS_Request_Create DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Create.name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Create.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
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
internal record CToS_Request_Join(string own_name, string opp_name) : Common.PacketTable
{
	public static CToS_Request_Join DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header own_name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "own_name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Join.own_name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Join.own_name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header opp_name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "opp_name")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Join.opp_name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CToS_Request_Join.opp_name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		string own_name = Common.Common.DeserializeStr(ref bytes);
		string opp_name = Common.Common.DeserializeStr(ref bytes);
		return new(own_name, opp_name);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header own_name */
		bytes.AddRange(Common.Common.SerializeName("own_name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header opp_name */
		bytes.AddRange(Common.Common.SerializeName("opp_name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data own_name */
		bytes.AddRange(Common.Common.SerializeStr(own_name));
		/* Data opp_name */
		bytes.AddRange(Common.Common.SerializeStr(opp_name));
		return bytes;
	}
}
internal record CToS_Request_Start(CardGameUtils.Base.Deck decklist, bool no_shuffle) : Common.PacketTable
{
	public static CToS_Request_Start DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header decklist */
		{
			if(!Common.Common.DeserializeName(ref bytes, "decklist")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Start.decklist hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Request_Start.decklist, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		/* Field Header no_shuffle */
		{
			if(!Common.Common.DeserializeName(ref bytes, "no_shuffle")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_Start.no_shuffle hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for CToS_Request_Start.no_shuffle, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		CardGameUtils.Base.Deck decklist = CardGameUtils.Base.Deck.DeserializeInternal(ref bytes);
		bool no_shuffle = Common.Common.DeserializeBool(ref bytes);
		return new(decklist, no_shuffle);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header decklist */
		bytes.AddRange(Common.Common.SerializeName("decklist")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Field Header no_shuffle */
		bytes.AddRange(Common.Common.SerializeName("no_shuffle")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data decklist */
		bytes.AddRange(decklist.SerializeInternal());
		/* Data no_shuffle */
		bytes.AddRange(Common.Common.SerializeBool(no_shuffle));
		return bytes;
	}
}
