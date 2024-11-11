using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Replay;

#nullable enable
#pragma warning disable CS8981

public record Replay(int seed, List<string> cmdline_args, List<ReplayPacket> packets) : Common.PacketTable
{
	public byte[] Deserialize()
	{
		List<byte> dataBytes = DeserializeInternal();
		return [.. Common.Common.DeserializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.DeserializeN16(2) /* ProtoVersion */,
			.. Common.Common.DeserializeN16(1) /* SchemaVersion */,
			.. Common.Common.DeserializeName("Replay") /* Name */,
			.. dataBytes /* Root */];
	}

	public static Replay Serialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.SerializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return SerializeImpl(ref bytes);
	}
	public static Replay Serialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.SerializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return SerializeImpl(ref bytes);
	}
	private static Replay SerializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.SerializeName(ref bytes, "Replay"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		Replay ret = SerializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static Replay SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header seed */
		{
			if(!Common.Common.SerializeName(ref bytes, "seed")) /* Name */
			{
				throw new Exception("Field Header Replay.seed hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for Replay.seed, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header cmdline_args */
		{
			if(!Common.Common.SerializeName(ref bytes, "cmdline_args")) /* Name */
			{
				throw new Exception("Field Header Replay.cmdline_args hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Replay.cmdline_args, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header packets */
		{
			if(!Common.Common.SerializeName(ref bytes, "packets")) /* Name */
			{
				throw new Exception("Field Header Replay.packets hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Replay.packets, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		int seed = Common.Common.SerializeI32(ref bytes);
		byte cmdline_argsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(cmdline_argsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cmdline_args, expected 0, got {cmdline_argsNestingLevel}");
		}
		uint cmdline_argsCount = Common.Common.SerializeN32(ref bytes);
		List<string> cmdline_args = new((int)cmdline_argsCount);
		for(int cmdline_args_ = 0; cmdline_args_ < cmdline_args.Capacity; cmdline_args_++)
		{
			cmdline_args.Add(Common.Common.SerializeStr(ref bytes));
		}
		byte packetsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(packetsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for packets, expected 0, got {packetsNestingLevel}");
		}
		uint packetsCount = Common.Common.SerializeN32(ref bytes);
		List<ReplayPacket> packets = new((int)packetsCount);
		for(int packets_ = 0; packets_ < packets.Capacity; packets_++)
		{
			packets.Add(ReplayPacket.SerializeInternal(ref bytes));
		}
		return new(seed, cmdline_args, packets);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header seed */
		bytes.AddRange(Common.Common.DeserializeName("seed")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header cmdline_args */
		bytes.AddRange(Common.Common.DeserializeName("cmdline_args")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header packets */
		bytes.AddRange(Common.Common.DeserializeName("packets")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data seed */
		bytes.AddRange(Common.Common.DeserializeI32(seed));
		/* Data cmdline_args */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)cmdline_args.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cmdline_args_ in cmdline_args)
		{
			bytes.AddRange(Common.Common.DeserializeStr(cmdline_args_));
		}
		/* Data packets */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)packets.Count)); /* Count */
		/* Nesting Counts */
		foreach(var packets_ in packets)
		{
			bytes.AddRange(packets_.DeserializeInternal());
		}
		return bytes;
	}
}
public record ReplayPacket(int player, ReplayContent content) : Common.PacketTable
{
	public static ReplayPacket SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header player */
		{
			if(!Common.Common.SerializeName(ref bytes, "player")) /* Name */
			{
				throw new Exception("Field Header ReplayPacket.player hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for ReplayPacket.player, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header content */
		{
			if(!Common.Common.SerializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header ReplayPacket.content hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for ReplayPacket.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		int player = Common.Common.SerializeI32(ref bytes);
		ReplayContent content = ReplayContent.SerializeInternal(ref bytes);
		return new(player, content);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header player */
		bytes.AddRange(Common.Common.DeserializeName("player")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header content */
		bytes.AddRange(Common.Common.DeserializeName("content")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data player */
		bytes.AddRange(Common.Common.DeserializeI32(player));
		/* Data content */
		bytes.AddRange(content.DeserializeInternal());
		return bytes;
	}
}
public interface ReplayContent : Common.PacketUnion
{
	public static ReplayContent SerializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.DeserializeName("ctos")))
		{
			return ctos.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("stoc")))
		{
			return stoc.SerializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in ReplayContent");
		}
	}

	public record ctos(CardGameUtils.Structs.Duel.CToS_Content value) : ReplayContent
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("ctos")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static ctos SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for ReplayContent/ctos, expected `{(byte)(Common.TypeBytes.Union)}`, got `{type}`");
			}
			CardGameUtils.Structs.Duel.CToS_Content value = CardGameUtils.Structs.Duel.CToS_Content.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record stoc(CardGameUtils.Structs.Duel.SToC_Content value) : ReplayContent
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("stoc")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static stoc SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for ReplayContent/stoc, expected `{(byte)(Common.TypeBytes.Union)}`, got `{type}`");
			}
			CardGameUtils.Structs.Duel.SToC_Content value = CardGameUtils.Structs.Duel.SToC_Content.SerializeInternal(ref bytes);
			return new(value);
		}
	}
}
