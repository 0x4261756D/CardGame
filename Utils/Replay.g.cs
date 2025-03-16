using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CardGameUtils.Replay;

#nullable enable
#pragma warning disable CS8981
internal record Replay(int seed, List<string> cmdline_args, List<ReplayPacket> packets) : Common.PacketTable
{
	public byte[] Serialize()
	{
		List<byte> dataBytes = SerializeInternal();
		return [.. Common.Common.SerializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.SerializeN16(2) /* ProtoVersion */,
			.. Common.Common.SerializeN16(1) /* SchemaVersion */,
			.. Common.Common.SerializeName("Replay") /* Name */,
			.. dataBytes /* Root */];
	}

	public static Replay Deserialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.DeserializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return DeserializeImpl(ref bytes);
	}
	public static Replay Deserialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return DeserializeImpl(ref bytes);
	}
	public static async Task<Replay> DeserializeAsync(Stream stream, CancellationToken token)
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
	private static Replay DeserializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.DeserializeName(ref bytes, "Replay"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		Replay ret = DeserializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static Replay DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header seed */
		{
			if(!Common.Common.DeserializeName(ref bytes, "seed")) /* Name */
			{
				throw new Exception("Field Header Replay.seed hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for Replay.seed, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header cmdline_args */
		{
			if(!Common.Common.DeserializeName(ref bytes, "cmdline_args")) /* Name */
			{
				throw new Exception("Field Header Replay.cmdline_args hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Replay.cmdline_args, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header packets */
		{
			if(!Common.Common.DeserializeName(ref bytes, "packets")) /* Name */
			{
				throw new Exception("Field Header Replay.packets hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Replay.packets, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		int seed = Common.Common.DeserializeI32(ref bytes);
		byte cmdline_argsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(cmdline_argsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cmdline_args, expected 0, got {cmdline_argsNestingLevel}");
		}
		uint cmdline_argsCount = Common.Common.DeserializeN32(ref bytes);
		List<string> cmdline_args = new((int)cmdline_argsCount);
		for(int cmdline_args_ = 0; cmdline_args_ < cmdline_args.Capacity; cmdline_args_++)
		{
			cmdline_args.Add(Common.Common.DeserializeStr(ref bytes));
		}
		byte packetsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(packetsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for packets, expected 0, got {packetsNestingLevel}");
		}
		uint packetsCount = Common.Common.DeserializeN32(ref bytes);
		List<ReplayPacket> packets = new((int)packetsCount);
		for(int packets_ = 0; packets_ < packets.Capacity; packets_++)
		{
			packets.Add(ReplayPacket.DeserializeInternal(ref bytes));
		}
		return new(seed, cmdline_args, packets);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header seed */
		bytes.AddRange(Common.Common.SerializeName("seed")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header cmdline_args */
		bytes.AddRange(Common.Common.SerializeName("cmdline_args")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header packets */
		bytes.AddRange(Common.Common.SerializeName("packets")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data seed */
		bytes.AddRange(Common.Common.SerializeI32(seed));
		/* Data cmdline_args */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)cmdline_args.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cmdline_args_ in cmdline_args)
		{
			bytes.AddRange(Common.Common.SerializeStr(cmdline_args_));
		}
		/* Data packets */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)packets.Count)); /* Count */
		/* Nesting Counts */
		foreach(var packets_ in packets)
		{
			bytes.AddRange(packets_.SerializeInternal());
		}
		return bytes;
	}
}
internal record ReplayPacket(int player, ReplayContent content) : Common.PacketTable
{
	public static ReplayPacket DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header player */
		{
			if(!Common.Common.DeserializeName(ref bytes, "player")) /* Name */
			{
				throw new Exception("Field Header ReplayPacket.player hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for ReplayPacket.player, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header content */
		{
			if(!Common.Common.DeserializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header ReplayPacket.content hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for ReplayPacket.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		int player = Common.Common.DeserializeI32(ref bytes);
		ReplayContent content = ReplayContent.DeserializeInternal(ref bytes);
		return new(player, content);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header player */
		bytes.AddRange(Common.Common.SerializeName("player")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header content */
		bytes.AddRange(Common.Common.SerializeName("content")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data player */
		bytes.AddRange(Common.Common.SerializeI32(player));
		/* Data content */
		bytes.AddRange(content.SerializeInternal());
		return bytes;
	}
}
internal interface ReplayContent : Common.PacketUnion
{
	public static ReplayContent DeserializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.SerializeName("ctos")))
		{
			return ctos.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("stoc")))
		{
			return stoc.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in ReplayContent");
		}
	}

	internal record ctos(CardGameUtils.Structs.Duel.CToS_Content value) : ReplayContent
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("ctos")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static ctos DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for ReplayContent/ctos, expected `{(byte)(Common.TypeBytes.Union)}`, got `{type}`");
			}
			CardGameUtils.Structs.Duel.CToS_Content value = CardGameUtils.Structs.Duel.CToS_Content.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record stoc(CardGameUtils.Structs.Duel.SToC_Content value) : ReplayContent
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("stoc")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static stoc DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for ReplayContent/stoc, expected `{(byte)(Common.TypeBytes.Union)}`, got `{type}`");
			}
			CardGameUtils.Structs.Duel.SToC_Content value = CardGameUtils.Structs.Duel.SToC_Content.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
}
