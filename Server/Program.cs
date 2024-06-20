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
using CardGameUtils;
using CardGameUtils.ServerClientToServer;
using CardGameUtils.Structs;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using static CardGameUtils.ServerServerToClient.Artworks.Types;

namespace CardGameServer;

class Program
{
	public static Dictionary<string, Info> artworks = [];
	public static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
	public static ServerConfig config = new(additional_cards_path: "additional_cards/", artwork_path: null, port: 7043, room_min_port: 37042, room_max_port: 39942, core_info: new CoreInfo());
	public static DateTime lastAdditionalCardsDateTime;
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
		if(File.Exists(config.additional_cards_path))
		{
			lastAdditionalCardsDateTime = CardGameUtils.ServerServerToClient.AdditionalCards.Parser.ParseFrom(File.ReadAllBytes(config.additional_cards_path)).Timestamp.ToDateTime();
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
					Packet packet = Packet.Parser.ParseDelimitedFrom(stream);
					Functions.Log("Server received a request", includeFullPath: true);
					decision = HandlePacket(packet, stream);
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
					Packet? packet = TryReceiveRawPacket(player.stream, 100);
					if(packet is not null)
					{
						switch(packet.KindCase)
						{
							case Packet.KindOneofCase.Leave:
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
											new CardGameUtils.ServerServerToClient.Packet
											{
												OpponentChanged = new()
											}.WriteDelimitedTo(room.players[1 - playerIndex]?.stream);
										}
										catch(IOException e)
										{
											Functions.Log($"Could not send OpponentChangedResponse: {e.Message}");
										}
									}
								}
							}
							break;
							case Packet.KindOneofCase.Start:
							{
								Start request = packet.Start;
								Functions.Log("----START REQUEST HANDLING----", includeFullPath: true);
								if(request.Decklist.Count != GameConstants.DECK_SIZE + 3)
								{
									new CardGameUtils.ServerServerToClient.Start
									{
										Failure = new()
										{
											Reason = $"Your deck has the wrong size, {request.Decklist.Count} instead of {GameConstants.DECK_SIZE + 3}"
										}
									}.WriteDelimitedTo(player.stream);
									break;
								}
								Functions.Log("Player: " + playerIndex, includeFullPath: true);
								player.ready = true;
								player.noshuffle = request.Noshuffle;
								player.Decklist = [.. request.Decklist];
								if(room.players[1 - playerIndex] == null)
								{
									Functions.Log("No opponent", includeFullPath: true);
									new CardGameUtils.ServerServerToClient.Start
									{
										Failure = new()
										{
											Reason = $"You have no opponent"
										}
									}.WriteDelimitedTo(player.stream);
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
												new CardGameUtils.ServerServerToClient.Start
												{
													SuccessButWaiting = new()
												}.WriteDelimitedTo(p.stream);
											}
										}
									}
									else
									{
										Functions.Log("Could not create the core", severity: Functions.LogSeverity.Error, includeFullPath: true);
										new CardGameUtils.ServerServerToClient.Start
										{
											Failure = new()
											{
												Reason = "Could not create a core"
											}
										}.WriteDelimitedTo(player.stream);
									}
								}
								else
								{
									Functions.Log("Opponent not ready", includeFullPath: true);
									new CardGameUtils.ServerServerToClient.Start
									{
										Failure = new()
										{
											Reason = "Your opponent isn't ready yet"
										}
									}.WriteDelimitedTo(player.stream);
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

	private static Packet? TryReceiveRawPacket(NetworkStream stream, int timeoutMilliseconds)
	{
		if(!stream.CanRead)
		{
			return null;
		}
		Stopwatch watch = Stopwatch.StartNew();
		while(!stream.DataAvailable)
		{
			Thread.Sleep(timeoutMilliseconds);
			if(!stream.CanRead || (timeoutMilliseconds != -1 && timeoutMilliseconds < watch.ElapsedMilliseconds))
			{
				return null;
			}
		}
		return Packet.Parser.ParseDelimitedFrom(stream);
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

	private static HandlePacketReturn HandlePacket(Packet packet, NetworkStream stream)
	{
		CleanupRooms();
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		switch(packet.KindCase)
		{
			case Packet.KindOneofCase.Create:
			{
				string name = packet.Create.Name;
				if(string.IsNullOrWhiteSpace(name))
				{
					new CardGameUtils.ServerServerToClient.Create
					{
						Result = new() { Error = new() { Reason = "Could not create a core" } }
					}.WriteDelimitedTo(stream);
				}
				else
				{
					if(waitingList.Exists(x => x.players[0]?.Name == name || x.players[1]?.Name == name))
					{
						new CardGameUtils.ServerServerToClient.Create
						{
							Result = new() { Error = new() { Reason = "Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)" } }
						}.WriteDelimitedTo(stream);
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
							new CardGameUtils.ServerServerToClient.Create
							{
								Result = new() { Error = new() { Reason = "No free port found" } }
							}.WriteDelimitedTo(stream);
						}
						else
						{
							waitingList.Add(new Room(name, id, currentPort, stream));
							new CardGameUtils.ServerServerToClient.Create
							{
								Result = new() { Ok = new() }
							}.WriteDelimitedTo(stream);
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case Packet.KindOneofCase.Join:
			{
				Join request = packet.Join;
				if(string.IsNullOrWhiteSpace(request.OwnName) || string.IsNullOrWhiteSpace(request.OppName))
				{
					new CardGameUtils.ServerServerToClient.Join
					{
						Result = new() { Error = new() { Reason = "Names can't be empty" } }
					}.WriteDelimitedTo(stream);
				}
				else
				{
					if(waitingList.FindIndex(x => x.players[0]?.Name == request.OwnName || x.players[1]?.Name == request.OwnName) != -1)
					{
						new CardGameUtils.ServerServerToClient.Create
						{
							Result = new() { Error = new() { Reason = "Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)" } }
						}.WriteDelimitedTo(stream);
					}
					else
					{
						int index = waitingList.FindIndex(x => x.players[0]?.Name == request.OppName || x.players[1]?.Name == request.OppName);
						if(index == -1)
						{
							new CardGameUtils.ServerServerToClient.Create
							{
								Result = new() { Error = new() { Reason = "No player with that name is hosting a game right now" } }
							}.WriteDelimitedTo(stream);
						}
						else
						{
							string id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(seed + request.OwnName))).Replace("-", "");
							int playerIndex = waitingList[index].players[0] == null ? 0 : 1;
							waitingList[index].players[playerIndex] = new Room.Player(Name: request.OwnName, id: id, stream: stream, ready: false, noshuffle: false);
							if(waitingList[index].players[1 - playerIndex]!.stream != null && waitingList[index].players[1 - playerIndex]!.stream.Socket.Connected)
							{
								new CardGameUtils.ServerServerToClient.Packet
								{
									OpponentChanged = new()
									{
										Name = request.OwnName
									}
								}.WriteDelimitedTo(waitingList[index].players[1 - playerIndex]!.stream);
							}
							new CardGameUtils.ServerServerToClient.Create
							{
								Result = new() { Ok = new() }
							}.WriteDelimitedTo(stream);
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case Packet.KindOneofCase.Rooms:
			{
				if(waitingList.Exists(x => x.players[0]?.Name == null && x.players[1]?.Name == null))
				{
					Functions.Log($"There is a player whose name is null", severity: Functions.LogSeverity.Error, includeFullPath: true);
					return HandlePacketReturn.Continue;
				}
				CardGameUtils.ServerServerToClient.Rooms payload = new();
				payload.Rooms_.AddRange(waitingList.FindAll(x => !Array.TrueForAll(x.players, y => y?.ready ?? false)).ConvertAll(x => x.players[0]?.Name ?? x.players[1]?.Name));
				payload.WriteDelimitedTo(stream);
			}
			break;
			case Packet.KindOneofCase.AdditionalCards:
			{
				string fullAdditionalCardsPath = Path.Combine(baseDir, config.additional_cards_path);
				if(!File.Exists(fullAdditionalCardsPath) || CardGameUtils.ServerServerToClient.AdditionalCards.Parser.ParseFrom(File.ReadAllBytes(config.additional_cards_path)).Timestamp.ToDateTime() > lastAdditionalCardsDateTime)
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
				if(File.Exists(fullAdditionalCardsPath))
				{
					CardGameUtils.ServerServerToClient.AdditionalCards response = CardGameUtils.ServerServerToClient.AdditionalCards.Parser.ParseFrom(File.ReadAllBytes(config.additional_cards_path));
					lastAdditionalCardsDateTime = response.Timestamp.ToDateTime();
					response.WriteDelimitedTo(stream);
				}
				else
				{
					Functions.Log("No additional cards file exists", severity: Functions.LogSeverity.Warning);
					new CardGameUtils.ServerServerToClient.AdditionalCards
					{
						Timestamp = DateTime.Now.ToTimestamp()
					}.WriteDelimitedTo(stream);
				}
			}
			break;
			case Packet.KindOneofCase.Artworks:
			{
				if(config.artwork_path is null)
				{
					new CardGameUtils.ServerServerToClient.Artworks
					{
						SupportsArtworks = false
					}.WriteDelimitedTo(stream);
					break;
				}
				CardGameUtils.ServerServerToClient.Artworks response = new()
				{
					SupportsArtworks = true
				};
				foreach(string name in packet.Artworks.Names)
				{
					string sanitizedName = Functions.CardnameToFilename(name);
					if(artworks.TryGetValue(sanitizedName, out Info? info) && info is not null)
					{
						response.Artworks_[sanitizedName] = info;
					}
					else
					{
						string pngPath = Path.Combine(config.artwork_path, sanitizedName + ".png");
						string jpgPath = Path.Combine(config.artwork_path, sanitizedName + ".jpg");
						if(File.Exists(pngPath))
						{
							artworks[sanitizedName] = new Info { Filetype = Filetype.Png, Data = ByteString.CopyFrom(File.ReadAllBytes(pngPath)) };
						}
						else if(File.Exists(jpgPath))
						{
							artworks[sanitizedName] = new Info { Filetype = Filetype.Jpg, Data = ByteString.CopyFrom(File.ReadAllBytes(jpgPath)) };
						}
						else
						{
							artworks[sanitizedName] = new Info { Filetype = Filetype.Unknown };
						}
					}
				}
				response.WriteDelimitedTo(stream);
			}
			break;
			default:
			{
				throw new Exception($"ERROR: Unable to process this packet: Packet type: {packet.GetType()}");
			}
		}
		return HandlePacketReturn.Continue;
	}
}
