using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CardGameUtils.GameEnumsAndStructs;

#nullable enable
#pragma warning disable CS8981
internal record foo(byte dummy) : Common.PacketTable
{
	public byte[] Serialize()
	{
		List<byte> dataBytes = SerializeInternal();
		return [.. Common.Common.SerializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.SerializeN16(2) /* ProtoVersion */,
			.. Common.Common.SerializeN16(1) /* SchemaVersion */,
			.. Common.Common.SerializeName("foo") /* Name */,
			.. dataBytes /* Root */];
	}

	public static foo Deserialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.DeserializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return DeserializeImpl(ref bytes);
	}
	public static foo Deserialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return DeserializeImpl(ref bytes);
	}
	public static async Task<foo> DeserializeAsync(Stream stream, CancellationToken token)
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
	private static foo DeserializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.DeserializeName(ref bytes, "foo"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		foo ret = DeserializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static foo DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header dummy */
		{
			if(!Common.Common.DeserializeName(ref bytes, "dummy")) /* Name */
			{
				throw new Exception("Field Header foo.dummy hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N8))
			{
				throw new Exception($"Wrong field type for foo.dummy, expected {(byte)(Common.TypeBytes.N8)}, got {type}");
			}
		}
		byte dummy = Common.Common.DeserializeN8(ref bytes);
		return new(dummy);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header dummy */
		bytes.AddRange(Common.Common.SerializeName("dummy")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N8)); /* Type */
		/* Data dummy */
		bytes.AddRange(Common.Common.SerializeN8(dummy));
		return bytes;
	}
}
internal enum Location
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
internal enum PlayerClass
{
	UNKNOWN,
	All,
	Cultist,
	Pyromancer,
	Artificer,
	Gladiator,
}
internal enum GameResult
{
	Won,
	Lost,
	Draw,
}
