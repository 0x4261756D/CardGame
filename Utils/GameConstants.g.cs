using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.GameConstants;

#nullable enable

public record foo(byte dummy) : Common.PacketTable
{
	public byte[] Deserialize()
	{
		List<byte> dataBytes = DeserializeInternal();
		return [.. Common.Common.DeserializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.DeserializeN16(2) /* ProtoVersion */,
			.. Common.Common.DeserializeN16(1) /* SchemaVersion */,
			.. Common.Common.DeserializeName("foo") /* Name */,
			.. dataBytes /* Root */];
	}

	public static foo Serialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.SerializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return SerializeImpl(ref bytes);
	}
	public static foo Serialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.SerializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return SerializeImpl(ref bytes);
	}
	private static foo SerializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.SerializeName(ref bytes, "foo"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		foo ret = SerializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static foo SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header dummy */
		{
			if(!Common.Common.SerializeName(ref bytes, "dummy")) /* Name */
			{
				throw new Exception("Field Header foo.dummy hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N8))
			{
				throw new Exception($"Wrong field type for foo.dummy, expected {(byte)(Common.TypeBytes.N8)}, got {type}");
			}
		}
		byte dummy = Common.Common.SerializeN8(ref bytes);
		return new(dummy);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header dummy */
		bytes.AddRange(Common.Common.DeserializeName("dummy")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N8)); /* Type */
		/* Data dummy */
		bytes.AddRange(Common.Common.DeserializeN8(dummy));
		return bytes;
	}
}
public enum Location
{
	UNKNOWN,
	Any,
	Deck,
	Hand,
	Field,
	Grave,
	Quest,
	Ability,
}
public enum PlayerClass
{
	UNKNOWN,
	All,
	Cultist,
	Pyromancer,
	Artificer,
	Gladiator,
}
public enum GameResult
{
	Won,
	Lost,
	Draw,
}
