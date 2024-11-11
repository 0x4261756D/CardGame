using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System;

namespace Common;

public enum TypeBytes : byte
{
	ListFlag = 0b0010_0000,
	OptionalFlag = 0b0001_0000,
	I8 = 0b0000_0000,
	I16 = 0b0000_0001,
	I32 = 0b0000_0010,
	I64 = 0b0000_0011,
	N8 = 0b0000_0100,
	N16 = 0b0000_0101,
	N32 = 0b0000_0110,
	N64 = 0b0000_0111,
	Bool = 0b0000_1000,
	Str = 0b0000_1001,
	Enum = 0b0000_1011,
	Union = 0b0000_1100,
	Table = 0b0000_1101,
	Void = 0b1000_0000,
}

public class Common
{
	public static byte[] DeserializeI64(long s)
	{
		byte[] bytes = BitConverter.GetBytes(s);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}
	public static long SerializeI64(ref Span<byte> bytes)
	{
		Span<byte> n = bytes[..8];
		if(!BitConverter.IsLittleEndian)
		{
			n.Reverse();
		}
		bytes = bytes[8..];
		return BitConverter.ToInt64(n);
	}
	public static byte[] DeserializeN64(ulong s)
	{
		byte[] bytes = BitConverter.GetBytes(s);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}
	public static ulong SerializeN64(ref Span<byte> bytes)
	{
		Span<byte> n = bytes[..8];
		if(!BitConverter.IsLittleEndian)
		{
			n.Reverse();
		}
		bytes = bytes[8..];
		return BitConverter.ToUInt64(n);
	}
	public static byte[] DeserializeI32(int s)
	{
		byte[] bytes = BitConverter.GetBytes(s);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}
	public static int SerializeI32(ref Span<byte> bytes)
	{
		Span<byte> n = bytes[..4];
		if(!BitConverter.IsLittleEndian)
		{
			n.Reverse();
		}
		bytes = bytes[4..];
		return BitConverter.ToInt32(n);
	}
	public static byte[] DeserializeN32(uint s)
	{
		byte[] bytes = BitConverter.GetBytes(s);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}
	public static uint SerializeN32(ref Span<byte> bytes)
	{
		Span<byte> n = bytes[..4];
		if(!BitConverter.IsLittleEndian)
		{
			n.Reverse();
		}
		bytes = bytes[4..];
		return BitConverter.ToUInt32(n);
	}
	public static byte[] DeserializeI16(short s)
	{
		byte[] bytes = BitConverter.GetBytes(s);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}
	public static short SerializeI16(ref Span<byte> bytes)
	{
		Span<byte> n = bytes[..2];
		if(!BitConverter.IsLittleEndian)
		{
			n.Reverse();
		}
		bytes = bytes[2..];
		return BitConverter.ToInt16(n);
	}
	public static byte[] DeserializeN16(ushort s)
	{
		byte[] bytes = BitConverter.GetBytes(s);
		if(!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}
	public static ushort SerializeN16(ref Span<byte> bytes)
	{
		Span<byte> n = bytes[..2];
		if(!BitConverter.IsLittleEndian)
		{
			n.Reverse();
		}
		bytes = bytes[2..];
		return BitConverter.ToUInt16(n);
	}
	public static byte[] DeserializeI8(sbyte s)
	{
		return [(byte)s];
	}
	public static sbyte SerializeI8(ref Span<byte> bytes)
	{
		sbyte n = (sbyte)bytes[0];
		bytes = bytes[1..];
		return n;
	}
	public static byte[] DeserializeN8(byte s)
	{
		return [s];
	}
	public static byte SerializeN8(ref Span<byte> bytes)
	{
		byte n = bytes[0];
		bytes = bytes[1..];
		return n;
	}
	public static byte[] DeserializeBool(bool s)
	{
		return [(byte)(s ? 1 : 0)];
	}
	public static bool SerializeBool(ref Span<byte> bytes)
	{
		byte n = bytes[0];
		if(n > 1)
		{
			throw new Exception($"Could not serialize bool, value is greater than 1 ({n})");
		}
		bytes = bytes[1..];
		return n == 1;
	}
	public static byte[] DeserializeName(string name, int len = 4)
	{
		return Shake256.HashData(Encoding.UTF8.GetBytes(name), len);
	}
	public static bool SerializeName(ref Span<byte> bytes, string s, int len = 4)
	{
		bool ret = bytes[..len].SequenceEqual(DeserializeName(s, len: len));
		bytes = bytes[len..];
		return ret;
	}
	public static List<byte> DeserializeStr(string s)
	{
		List<byte> bytes = [];
		byte[] utf8Bytes = Encoding.UTF8.GetBytes(s);
		// Size
		if(utf8Bytes.Length > 0xffffff)
		{
			throw new Exception($"String length too big: {utf8Bytes.Length} > {0xffffff}");
		}
		bytes.AddRange(DeserializeN32((uint)utf8Bytes.Length)[..3]);
		bytes.AddRange(utf8Bytes);
		return bytes;
	}
	public static string SerializeStr(ref Span<byte> bytes)
	{
		Span<byte> n = [.. bytes[..3], 0]; // NOTE: DeserializeN32 can't be used here because the size is only 3 bytes and since BitConverter.ToUInt32 needs 4 bytes this awkward extending has to happen.
		if(!BitConverter.IsLittleEndian)
		{
			n.Reverse();
		}
		uint size = BitConverter.ToUInt32(n);
		bytes = bytes[3..];
		string ret = Encoding.UTF8.GetString(bytes[..(int)size]);
		bytes = bytes[(int)size..];
		return ret;
	}
}

#pragma warning disable IDE1006
public interface PacketType
{
	public List<byte> DeserializeInternal();
}
public interface PacketUnion : PacketType { }
public interface PacketTable : PacketType { }
