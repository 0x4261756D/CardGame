using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Google.Protobuf;

namespace CardGameUtils;

class Replay(string[] cmdlineArgs, int seed)
{
	public interface IPacket {}
	public class CtoS(DuelClientToServer.Packet packet) : IPacket
	{
		public DuelClientToServer.Packet packet = packet;
	}
	public class StoC(DuelServerToClient.Packet packet) : IPacket
	{
		public DuelServerToClient.Packet packet = packet;
	}
	[method: JsonConstructor]
	public class GameAction(int player, string packetContent, bool isClientToServer)
	{
		public int player = player;
		public bool isClientToServer = isClientToServer;
		public string packetContent = packetContent;
		public IPacket PacketContentToPacket()
		{
			return isClientToServer ? new CtoS(DuelClientToServer.Packet.Parser.ParseFrom(Convert.FromBase64String(packetContent))) : new StoC(DuelServerToClient.Packet.Parser.ParseFrom(Convert.FromBase64String(packetContent)));
		}

		public GameAction(int player, IPacket packet) :
			this(player, Convert.ToBase64String((packet is StoC stoc) ? stoc.packet.ToByteArray() : ((CtoS)packet).packet.ToByteArray()), packet is StoC)
		{
		}
	}
	public string[] cmdlineArgs = cmdlineArgs;
	public List<GameAction> actions = [];
	public int seed = seed;
}
