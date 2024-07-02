using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Google.ProtocolBuffers;
using CardGameUtils.Packets;

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
	private static partial Regex CardnameFilenameRegex();
	public static string CardnameToFilename(string name)
	{
		return CardnameFilenameRegex().Replace(name, "");
	}

	[GeneratedRegex(@"[^a-zA-Z0-9]")]
	private static partial Regex UsernameFilenameRegex();
	public static string UsernameToFilename(string name)
	{
		return UsernameFilenameRegex().Replace(name, "_");
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

	public static Packet DeserializePacket(byte[] data)
	{
		if(!Packet.VerifyPacket(data))
		{
			throw new Exception("Invalid packet received");
		}
		return Packet.GetRootAsPacket(new ByteBuffer(data));
	}

	public static Packet ReceivePacket(NetworkStream stream)
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

	public static bool IsInLocation(GameConstants.Location first, GameConstants.Location second)
	{
		return first == second || first == GameConstants.Location.Any || second == GameConstants.Location.Any;
	}
}

