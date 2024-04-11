using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardGameUtils;

class Replay(string[] cmdlineArgs, int seed)
{
	[method: JsonConstructor]
	public class GameAction(int player, string packetContent, bool clientToServer, uint packetVersion)
	{
		public int player = player;
		public uint packetVersion = packetVersion;
		public string packetContent = packetContent;
		public byte[] PacketContentBytes()
		{
			return Convert.FromBase64String(packetContent);
		}
		public bool clientToServer = clientToServer;

		public GameAction(int player, Structs.NetworkingStructs.Packet packet, bool clientToServer) :
			this(player, Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(packet, typeof(Structs.NetworkingStructs.Packet), options: GenericConstants.packetSerialization)), clientToServer, packet.version)
		{
		}
	}
	public string[] cmdlineArgs = cmdlineArgs;
	public List<GameAction> actions = [];
	public int seed = seed;
}
