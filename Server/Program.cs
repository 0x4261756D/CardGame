using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.Structs.Server;

namespace CardGameServer;

class Program
{
	public static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
	public static ServerConfig config = new(additional_cards_path: "additional_cards/", artwork_path: null, port: 7043, room_min_port: 37042, room_max_port: 39942, core_info: new CoreInfo());
	public static string? seed;
	public static SHA384 sha = SHA384.Create();
	// TODO: MAKE THIS THREAD-SAFE SOMEHOW
	public static List<Room> waitingList = [];
	static void Main(string[] args)
	{
		string? configLocation = null;
		for(int i = 0; i < args.Length; i++)
		{
			string[] parts = args[i].Split('=');
			if(parts.Length == 2)
			{
				switch(parts[0])
				{
					case "--config":
					case "-c":
						string path = Path.Combine(baseDir, parts[1]);
						if(File.Exists(path))
						{
							configLocation = path;
						}
						else
						{
							Functions.Log($"No config file found at {Path.GetFullPath(path)}.", severity: Functions.LogSeverity.Error);
							return;
						}
						break;
				}
			}
		}
		if(configLocation == null)
		{
			Functions.Log("Please provide a config file with '--config=path/to/config'", severity: Functions.LogSeverity.Error);
			return;
		}
		PlatformServerConfig platformConfig = JsonSerializer.Deserialize<PlatformServerConfig>(File.ReadAllText(configLocation), GenericConstants.platformServerConfigSerialization);
		if(Environment.OSVersion.Platform == PlatformID.Unix)
		{
			config = platformConfig.linux;
		}
		else
		{
			config = platformConfig.windows;
		}
		TcpListener listener = TcpListener.Create(config.port);
		byte[] nowBytes = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
		seed = Convert.ToBase64String(sha.ComputeHash(nowBytes));
		listener.Start();
		while(true)
		{
			if(listener.Pending())
			{
				Functions.Log("Server waiting for a connection", includeFullPath: true);
				TcpClient client = listener.AcceptTcpClient();
				Functions.Log("Server connected", includeFullPath: true);
				NetworkStream stream = client.GetStream();
				Functions.Log("Waiting for data", includeFullPath: true);
				HandlePacketReturn decision = HandlePacketReturn.Continue;
				try
				{
					CToS_Content content = CToS_Packet.Serialize(stream).content;
					Functions.Log("Server received a request", includeFullPath: true);
					decision = HandlePacket(content, stream);
					if(decision == HandlePacketReturn.Break)
					{
						Functions.Log("Server received a request signalling it should stop", includeFullPath: true);
						break;
					}
					Functions.Log("Server sent a response", includeFullPath: true);
				}
				catch(Exception e)
				{
					Functions.Log($"Exception while reading a message: {e}");
				}
				if(decision != HandlePacketReturn.ContinueKeepStream)
				{
					stream.Close();
					client.Close();
					client.Dispose();
					stream.Dispose();
				}
			}
			HandleRooms();
		}
		listener.Stop();
	}
	public static CToS_Content? TryReceivePacket(NetworkStream stream, int timeoutInMs)
	{
		try
		{
			Task<CToS_Packet> task = Task.Run(() => CToS_Packet.Serialize(stream));
			int i = Task.WaitAny(task, Task.Delay(timeoutInMs));
			return i == 0 ? task.Result.content : null;
		}
		catch(Exception e)
		{
			Functions.Log(e.Message, severity: Functions.LogSeverity.Warning);
			return null;
		}
	}

	private static void HandleRooms()
	{
		if(waitingList.Count == 0)
		{
			Thread.Sleep(100);
		}
		for(int roomIndex = waitingList.Count - 1; roomIndex >= 0; roomIndex--)
		{
			Room room = waitingList[roomIndex];
			for(int playerIndex = 0; playerIndex < waitingList[roomIndex].players.Length; playerIndex++)
			{
				Room.Player? player = room.players[playerIndex];
				if(player != null && player.stream != null &&
					player.stream.CanRead &&
					player.stream.DataAvailable)
				{
					CToS_Content? packet = TryReceivePacket(player.stream, 100);
					if(packet is not null)
					{
						switch(packet)
						{
							case CToS_Content.leave:
							{
								player.stream.Dispose();
								room.players[playerIndex] = null;
								if(room.players[0] == null && room.players[1] == null)
								{
									waitingList.RemoveAt(roomIndex);
									return;
								}
								else
								{
									if(room.players[1 - playerIndex]?.stream.Socket.Connected ?? false)
									{
										try
										{
											room.players[1 - playerIndex]?.stream.Write(new SToC_Packet(new SToC_Content.opponent_changed(new(null))).Deserialize());
										}
										catch(IOException e)
										{
											Functions.Log($"Could not send OpponentChangedResponse: {e.Message}");
										}
									}
								}
							}
							break;
							case CToS_Content.start r:
							{
								CToS_Request_Start request = r.value;
								Functions.Log("----START REQUEST HANDLING----", includeFullPath: true);
								if(request.decklist.cards.Count != GameConstantsElectricBoogaloo.DECK_SIZE)
								{
									player.stream.Write(new SToC_Packet(new SToC_Content.start(new SToC_Response_Start.failure("Your deck has the wrong size"))).Deserialize());
									break;
								}
								Functions.Log("Player: " + playerIndex, includeFullPath: true);
								player.ready = true;
								player.noshuffle = request.no_shuffle;
								player.Decklist = request.decklist;
								if(room.players[1 - playerIndex] == null)
								{
									Functions.Log("No opponent", includeFullPath: true);
									player.stream.Write(new SToC_Packet(new SToC_Content.start(new SToC_Response_Start.failure("You have no opponent"))).Deserialize());
									break;
								}
								Functions.Log("Opponent present", includeFullPath: true);
								if(Array.TrueForAll(waitingList[roomIndex].players, x => x?.ready ?? false))
								{
									Functions.Log("All players ready", includeFullPath: true);
									if(room.StartGame())
									{
										foreach(Room.Player? p in room.players)
										{
											if(p != null && p.stream.Socket.Connected)
											{
												p.stream.Write(new SToC_Packet(new SToC_Content.start(new SToC_Response_Start.success_but_waiting())).Deserialize());
											}
										}
									}
									else
									{
										Functions.Log("Could not create the core", severity: Functions.LogSeverity.Error, includeFullPath: true);
										foreach(Room.Player? p in room.players)
										{
											if(p is not null && p.stream.Socket.Connected)
											{
												p.stream.Write(new SToC_Packet(new SToC_Content.start(new SToC_Response_Start.failure("Could not create the core"))).Deserialize());
											}
										}
									}
								}
								else
								{
									Functions.Log("Opponent not ready", includeFullPath: true);
									player.stream.Write(new SToC_Packet(new SToC_Content.start(new SToC_Response_Start.failure("Opponent not ready"))).Deserialize());
								}
								Functions.Log("----END----", includeFullPath: true);
							}
							break;
						}
					}
				}
			}
		}
	}

	private enum HandlePacketReturn
	{
		Break,
		Continue,
		ContinueKeepStream,
	}

	private static void CleanupRooms()
	{
		int waitingCount = 0;
		for(int i = waitingList.Count - 1; i >= 0; i--)
		{
			if((DateTime.Now - waitingList[i].startTime).Days > 1 || waitingList[i].isFinished)
			{
				waitingList[i].players[0]?.stream.Close();
				waitingList[i].players[1]?.stream.Close();
				waitingList.RemoveAt(i);
				waitingCount++;
			}
		}
		Functions.Log($"Cleaned up {waitingCount} abandoned waiting rooms, {waitingList.Count} rooms still open", includeFullPath: true);
	}

	private static HandlePacketReturn HandlePacket(CToS_Content content, NetworkStream stream)
	{
		CleanupRooms();
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		SToC_Content reply;
		switch(content)
		{
			case CToS_Content.create request:
			{
				string name = request.value.name;
				if(string.IsNullOrWhiteSpace(name))
				{
					reply = new SToC_Content.create(new(new ErrorOr.error(new("Names cannot be empty"))));
				}
				else
				{
					if(waitingList.Exists(x => x.players[0]?.Name == name || x.players[1]?.Name == name))
					{
						reply = new SToC_Content.create(new
						(
							new ErrorOr.error("Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)")
						));
					}
					else
					{
						string id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(seed + name))).Replace("-", "");
						int currentPort = -1;
						for(int i = config.room_min_port; i <= config.room_max_port; i++)
						{
							bool free = true;
							foreach(Room r in waitingList)
							{
								if(r.port == i)
								{
									free = false;
									break;
								}
							}
							if(free)
							{
								if(Array.Exists(IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections(), x => x.LocalEndPoint.Port == i))
								{
									free = false;
								}
							}
							if(free)
							{
								Functions.Log($"Next port: {i}");
								currentPort = i;
								break;
							}
						}
						if(currentPort == -1)
						{
							Functions.Log("No free port found", severity: Functions.LogSeverity.Warning);
							reply = new SToC_Content.create(new(new ErrorOr.error("No free port found")));
						}
						else
						{
							waitingList.Add(new Room(name, id, currentPort, stream));
							reply = new SToC_Content.create(new(new ErrorOr.success()));
							stream.Write(new SToC_Packet(reply).Deserialize());
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case CToS_Content.join request:
			{
				if(string.IsNullOrWhiteSpace(request.value.own_name) || string.IsNullOrWhiteSpace(request.value.opp_name))
				{
					reply = new SToC_Content.join(new(new ErrorOr.error("Names cannot be empty")));
				}
				else
				{
					if(waitingList.FindIndex(x => x.players[0]?.Name == request.value.own_name || x.players[1]?.Name == request.value.own_name) != -1)
					{
						reply = new SToC_Content.join(new
						(
							new ErrorOr.error("Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)")
						));
					}
					else
					{
						int index = waitingList.FindIndex(x => x.players[0]?.Name == request.value.opp_name || x.players[1]?.Name == request.value.opp_name);
						if(index == -1)
						{
							reply = new SToC_Content.join(new(new ErrorOr.error("No player with that name hosts a game right now")));
						}
						else
						{
							string id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(seed + request.value.own_name))).Replace("-", "");
							int playerIndex = waitingList[index].players[0] == null ? 0 : 1;
							waitingList[index].players[playerIndex] = new Room.Player(Name: request.value.own_name, id: id, stream: stream, ready: false, noshuffle: false);
							reply = new SToC_Content.join(new(new ErrorOr.success()));
							if(waitingList[index].players[1 - playerIndex]!.stream != null && waitingList[index].players[1 - playerIndex]!.stream.Socket.Connected)
							{
								waitingList[index].players[1 - playerIndex]!.stream.Write(new SToC_Packet(new SToC_Content.opponent_changed(new(request.value.own_name))).Deserialize());
							}
							stream.Write(new SToC_Packet(reply).Deserialize());
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case CToS_Content.rooms:
			{
				if(waitingList.Exists(x => x.players[0]?.Name == null && x.players[1]?.Name == null))
				{
					Functions.Log($"There is a player whose name is null", severity: Functions.LogSeverity.Error, includeFullPath: true);
					return HandlePacketReturn.Continue;
				}
				reply = new SToC_Content.rooms(new(waitingList.FindAll(x => !Array.TrueForAll(x.players, y => y?.ready ?? false)).ConvertAll(x => x.players[0]?.Name ?? x.players[1]?.Name ?? throw new Exception("Empty name, this should not happen"))));
			}
			break;
			case CToS_Content.additional_cards:
			{
				string fullAdditionalCardsPath = Path.Combine(baseDir, config.additional_cards_path);
				LetCoreGenerateAdditionalCards(fullAdditionalCardsPath);
				if(File.Exists(fullAdditionalCardsPath))
				{
					SToC_Content savedContent = SToC_Packet.Serialize(File.ReadAllBytes(fullAdditionalCardsPath)).content;
					if(savedContent is not SToC_Content.additional_cards)
					{
						Functions.Log($"Saved additional cards packet is of unexpected type {savedContent.GetType()}");
						reply = new SToC_Content.additional_cards(new(0, []));
					}
					else
					{
						reply = savedContent;
					}
				}
				else
				{
					reply = new SToC_Content.additional_cards(new(0, []));
					Functions.Log("No additional cards file exists", severity: Functions.LogSeverity.Warning);
				}
			}
			break;
			case CToS_Content.artworks request:
			{
				if(config.artwork_path is null)
				{
					reply = new SToC_Content.artworks(new([]));
					break;
				}
				List<Artwork> artworks = [];
				foreach(string name in request.value.names)
				{
					string sanitizedName = Functions.CardnameToFilename(name);
					string pngPath = Path.Combine(config.artwork_path, sanitizedName + ".png");
					string jpgPath = Path.Combine(config.artwork_path, sanitizedName + ".jpg");
					if(File.Exists(pngPath))
					{
						artworks.Add(new(name: sanitizedName, filetype: ArtworkFiletype.PNG, data: [.. File.ReadAllBytes(pngPath)]));
					}
					else if(File.Exists(jpgPath))
					{
						artworks.Add(new(name: sanitizedName, filetype: ArtworkFiletype.JPG, data: [.. File.ReadAllBytes(pngPath)]));
					}
					// Else just don't add it, the client MUST handle not receiving an artwork for every name requested.
				}
				reply = new SToC_Content.artworks(new(artworks));
			}
			break;
			default:
			{
				throw new Exception($"ERROR: Unable to process this packet: Packet type: {content.GetType()}");
			}
		}
		
		SToC_Packet p = new(reply);
		Functions.Log(string.Join(',', p.Deserialize()), Functions.LogSeverity.Warning);
		stream.Write(p.Deserialize());
		return HandlePacketReturn.Continue;
	}

	private static void LetCoreGenerateAdditionalCards(string fullAdditionalCardsPath)
	{
		ProcessStartInfo info = new()
		{
			Arguments = config.core_info.Arguments + " --additional_cards_path=" + fullAdditionalCardsPath,
			CreateNoWindow = config.core_info.CreateNoWindow,
			UseShellExecute = config.core_info.UseShellExecute,
			FileName = config.core_info.FileName,
			WorkingDirectory = config.core_info.WorkingDirectory,
		};
		_ = (Process.Start(info)?.WaitForExit(10000));
	}
}
