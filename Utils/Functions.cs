using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using static CardGameUtils.Structs.NetworkingStructs;

namespace CardGameUtils;

partial class Functions
{
	public enum LogSeverity
	{
		Debug,
		Warning,
		Error,
	}

	[GeneratedRegex(@"[^#\|a-zA-Z0-9]")]
	private static partial Regex CardFileNameRegex();
	public static string CardNameToFilename(string name)
	{
		return CardFileNameRegex().Replace(name, "");
	}

	public static void Log(string message, LogSeverity severity = LogSeverity.Debug, bool includeFullPath = false, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string propertyName = "")
	{
		ConsoleColor current = Console.ForegroundColor;
		if(severity == LogSeverity.Warning)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
		}
		else if(severity == LogSeverity.Error)
		{
			Console.ForegroundColor = ConsoleColor.Red;
		}
#if RELEASE
		if(severity != LogSeverity.Debug)
		{
#endif
		Console.WriteLine($"{severity.ToString().ToUpper()}: [{(includeFullPath ? propertyName : Path.GetFileNameWithoutExtension(propertyName))}:{lineNumber}]: {message}");
#if RELEASE
		}
#endif
		Console.ForegroundColor = current;
	}

	public static Packet DeserializeRaw(byte[] data)
	{
		// NOTE: IT IS OF UTMOST FUCKING IMPORTANCE THAT WE ALWAYS DE-/SERIALIZE Packet,
		//		 NOT THE SPECIFIC TYPE SINCE IT WILL NOT INCLUDE THE $type ATTRIBUTE AND LOSE ALL TYPE INFO OTHERWISE
		// This has cost approximately 2 hours of my life, staring at two seemingly identical pieces of code,
		// the only difference being the type passed, wondering why one works and the other not...
		return (Packet?)JsonSerializer.Deserialize(data, typeof(Packet), GenericConstants.packetSerialization) ?? throw new Exception("Deserialization returned null");
	}

	public static byte[] GeneratePayload(Packet data)
	{
		// NOTE: IT IS OF UTMOST FUCKING IMPORTANCE THAT WE ALWAYS DE-/SERIALIZE Packet,
		//		 NOT THE SPECIFIC TYPE SINCE IT WILL NOT INCLUDE THE $type ATTRIBUTE AND LOSE ALL TYPE INFO OTHERWISE
		// This has cost approximately 2 hours of my life, staring at two seemingly identical pieces of code,
		// the only difference being the type passed, wondering why one works and the other not...
		byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, typeof(Packet), GenericConstants.packetSerialization);
		return [.. BitConverter.GetBytes(bytes.Length), .. bytes];
	}
	public static byte[] ReadPacketBytes(NetworkStream stream)
	{
		byte[] buffer = new byte[4];
		stream.ReadExactly(buffer);
		uint length = BitConverter.ToUInt32(buffer);
		buffer = new byte[length];
		stream.ReadExactly(buffer);
		return buffer;
	}

	public static Packet ReceiveRawPacket(NetworkStream stream)
	{
		// NOTE: IT IS OF UTMOST FUCKING IMPORTANCE THAT WE ALWAYS DE-/SERIALIZE Packet,
		//		 NOT THE SPECIFIC TYPE SINCE IT WILL NOT INCLUDE THE $type ATTRIBUTE AND LOSE ALL TYPE INFO OTHERWISE
		// This has cost approximately 2 hours of my life, staring at two seemingly identical pieces of code,
		// the only difference being the type passed, wondering why one works and the other not...
		Packet packet = JsonSerializer.Deserialize<Packet>(ReadPacketBytes(stream), GenericConstants.packetSerialization) ?? throw new Exception("Deserialization returned null");
		packet.EnsureCompatible();
		return packet;
	}
	public static Packet? TryReceiveRawPacket(NetworkStream stream, long timeoutMs)
	{
		Stopwatch watch = Stopwatch.StartNew();
		if(!stream.CanRead)
		{
			return null;
		}
		while(!stream.DataAvailable)
		{
			Thread.Sleep(10);
			if(!stream.CanRead || (timeoutMs != -1 && watch.ElapsedMilliseconds > timeoutMs))
			{
				return null;
			}
		}
		// NOTE: IT IS OF UTMOST FUCKING IMPORTANCE THAT WE ALWAYS DE-/SERIALIZE Packet,
		//		 NOT THE SPECIFIC TYPE SINCE IT WILL NOT INCLUDE THE $type ATTRIBUTE AND LOSE ALL TYPE INFO OTHERWISE
		// This has cost approximately 2 hours of my life, staring at two seemingly identical pieces of code,
		// the only difference being the type passed, wondering why one works and the other not...
		Packet? packet = JsonSerializer.Deserialize<Packet>(ReadPacketBytes(stream), GenericConstants.packetSerialization);
		packet?.EnsureCompatible();
		return packet;
	}
	public static T? TryReceivePacket<T>(NetworkStream stream, long timeoutMs) where T : Packet
	{
		Stopwatch watch = Stopwatch.StartNew();
		if(!stream.CanRead)
		{
			return null;
		}
		while(!stream.DataAvailable)
		{
			Thread.Sleep(10);
			if(!stream.CanRead || (timeoutMs != -1 && watch.ElapsedMilliseconds > timeoutMs))
			{
				return null;
			}
		}
		Packet? packet;
		do
		{
			// NOTE: IT IS OF UTMOST FUCKING IMPORTANCE THAT WE ALWAYS DE-/SERIALIZE Packet,
			//		 NOT THE SPECIFIC TYPE SINCE IT WILL NOT INCLUDE THE $type ATTRIBUTE AND LOSE ALL TYPE INFO OTHERWISE
			// This has cost approximately 2 hours of my life, staring at two seemingly identical pieces of code,
			// the only difference being the type passed, wondering why one works and the other not...
			packet = JsonSerializer.Deserialize<Packet>(ReadPacketBytes(stream), GenericConstants.packetSerialization);
			packet?.EnsureCompatible();
		}
		while(packet is not T);

		return (T?)packet;
	}

	public static T ReceivePacket<T>(NetworkStream stream) where T : Packet
	{
		Packet packet;
		do
		{
			packet = ReceiveRawPacket(stream);
		}
		while(packet is not T);
		return (T)packet;
	}
	public static void Send(Packet request, string address, int port)
	{
		using TcpClient client = new(address, port);
		using NetworkStream stream = client.GetStream();
		stream.Write(GeneratePayload(request));
	}
	public static R SendAndReceive<R>(Packet request, string address, int port) where R : Packet
	{
		using TcpClient client = new(address, port);
		using NetworkStream stream = client.GetStream();
		stream.Write(GeneratePayload(request));
		return ReceivePacket<R>(stream);
	}

	public static string ArtworkFiletypeToExtension(ServerPackets.ArtworkFiletype filetype)
	{
		return filetype switch
		{
			ServerPackets.ArtworkFiletype.JPG => ".jpg",
			ServerPackets.ArtworkFiletype.PNG => ".png",
			_ => throw new NotImplementedException(),
		};
	}
}

