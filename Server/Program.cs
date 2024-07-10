using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CardGameUtils;
using CardGameUtils.Packets.Server;
using CardGameUtils.Structs;
using Thrift.Protocol;
using Thrift.Transport.Client;

namespace CardGameServer;

class Program
{
	public static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
	public static ServerConfig config = new(additional_cards_path: "additional_cards/", artwork_path: null, port: 7043, room_min_port: 37042, room_max_port: 39942, core_info: new CoreInfo());
	public static long lastAdditionalCardsTimestamp;
	public static string? seed;
	public static SHA384 sha = SHA384.Create();
	// TODO: MAKE THIS THREAD-SAFE SOMEHOW
	public static List<Room> waitingList = [];
	static async Task Main(string[] args)
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
		PlatformServerConfig platformConfig = JsonSerializer.Deserialize<PlatformServerConfig>(await File.ReadAllTextAsync(configLocation), GenericConstants.platformServerConfigSerialization);
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
			ServerAdditionalCards additionalCards = new();
			await additionalCards.ReadAsync(new TCompactProtocol(new Functions.TSimpleFileTransport(config.additional_cards_path, Functions.TSimpleFileTransport.OpenMode.Read)), default);
			lastAdditionalCardsTimestamp = additionalCards.Timestamp;
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
				TcpClient client = await listener.AcceptTcpClientAsync();
				Functions.Log("Server connected", includeFullPath: true);
				Functions.Log("Waiting for data", includeFullPath: true);
				HandlePacketReturn decision = HandlePacketReturn.Continue;
				try
				{
					ClientPacket packet = await ClientPacket.ReadAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
					Functions.Log("Server received a request", includeFullPath: true);
					decision = await HandlePacket(packet, client);
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
					client.Close();
					client.Dispose();
				}
			}
			await HandleRooms();
		}
		listener.Stop();
	}

	private static async Task HandleRooms()
	{
		if(waitingList.Count == 0)
		{
			await Task.Delay(100);
		}
		for(int roomIndex = waitingList.Count - 1; roomIndex >= 0; roomIndex--)
		{
			Room room = waitingList[roomIndex];
			for(int playerIndex = 0; playerIndex < waitingList[roomIndex].players.Length; playerIndex++)
			{
				Room.Player? player = room.players[playerIndex];
				if(player != null && player.client != null &&
					player.client.Connected &&
					player.client.Available > 0)
				{
					ClientPacket packet = await ClientPacket.ReadAsync(new TCompactProtocol(new TSocketTransport(player.client, new())), default);
					if(packet != null)
					{
						switch(packet)
						{
							case ClientPacket.leave:
							{
								player.client.Dispose();
								room.players[playerIndex] = null;
								if(room.players[0] == null && room.players[1] == null)
								{
									waitingList.RemoveAt(roomIndex);
									return;
								}
								else
								{
									if(room.players[1 - playerIndex]?.client.Connected ?? false)
									{
										try
										{
											await new ServerPacket.opponent_changed(new()).WriteAsync(new TCompactProtocol(new TSocketTransport(room.players[1 - playerIndex]?.client, new())), default);
										}
										catch(IOException e)
										{
											Functions.Log($"Could not send OpponentChangedResponse: {e.Message}");
										}
									}
								}
							}
							break;
							case ClientPacket.start:
							{
								ClientStart request = packet.As_start!;
								Functions.Log("----START REQUEST HANDLING----", includeFullPath: true);
								if(request.Decklist is null)
								{
									await new ServerPacket.start(new() { Result = new ServerStartResult.failure(new() { Result = "Missing deck" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(player.client, new())), default);
									break;
								}
								if(request.Decklist.Count != GameConstants.DECK_SIZE + 3)
								{
									await new ServerPacket.start(new() { Result = new ServerStartResult.failure(new() { Result = "Your deck has the wrong size" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(player.client, new())), default);
									break;
								}
								Functions.Log("Player: " + playerIndex, includeFullPath: true);
								player.ready = true;
								player.noshuffle = request.Noshuffle;
								player.Decklist = request.Decklist;
								if(room.players[1 - playerIndex] == null)
								{
									Functions.Log("No opponent", includeFullPath: true);
									await new ServerPacket.start(new() { Result = new ServerStartResult.failure(new() { Result = "You have no opponent" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(player.client, new())), default);
									break;
								}
								Functions.Log("Opponent present", includeFullPath: true);
								if(Array.TrueForAll(waitingList[roomIndex].players, x => x?.ready ?? false))
								{
									Functions.Log("All players ready", includeFullPath: true);
									if(await room.StartGame())
									{
										foreach(Room.Player? p in room.players)
										{
											if(p != null && p.client.Connected)
											{
												await new ServerPacket.start(new() { Result = new ServerStartResult.success_but_waiting(new()) }).WriteAsync(new TCompactProtocol(new TSocketTransport(p.client, new())), default);
											}
										}
									}
									else
									{
										Functions.Log("Could not create the core", severity: Functions.LogSeverity.Error, includeFullPath: true);
										await new ServerPacket.start(new() { Result = new ServerStartResult.failure(new() { Result = "Could not create core" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(player.client, new())), default);
									}
								}
								else
								{
									Functions.Log("Opponent not ready", includeFullPath: true);
									await new ServerPacket.start(new() { Result = new ServerStartResult.failure(new() { Result = "Your opponent isn't ready yet" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(player.client, new())), default);
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
				waitingList[i].players[0]?.client.Close();
				waitingList[i].players[1]?.client.Close();
				waitingList.RemoveAt(i);
				waitingCount++;
			}
		}
		Functions.Log($"Cleaned up {waitingCount} abandoned waiting rooms, {waitingList.Count} rooms still open", includeFullPath: true);
	}

	private static async Task<HandlePacketReturn> HandlePacket(ClientPacket packet, TcpClient client)
	{
		CleanupRooms();
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		switch(packet)
		{
			case ClientPacket.create:
			{
				string? name = packet.As_create!.Name;
				if(string.IsNullOrWhiteSpace(name))
				{
					await new ServerPacket.create(new() { Result = new Result.failure(new() { Result = "Names can't be empty" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
				}
				else
				{
					if(waitingList.Exists(x => x.players[0]?.Name == name || x.players[1]?.Name == name))
					{
						await new ServerPacket.create(new() { Result = new Result.failure(new() { Result = "Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
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
							await new ServerPacket.create(new() { Result = new Result.failure(new() { Result = "No free port found" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
						}
						else
						{
							waitingList.Add(new Room(name, id, currentPort, client));
							await new ServerPacket.create(new() { Result = new Result.success(new()) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case ClientPacket.join:
			{
				ClientJoin request = packet.As_join!;
				if(string.IsNullOrWhiteSpace(request.Own_name) || string.IsNullOrWhiteSpace(request.Own_name))
				{
					await new ServerPacket.join(new() { Result = new Result.failure(new() { Result = "Names can't be empty" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
				}
				else
				{
					if(waitingList.FindIndex(x => x.players[0]?.Name == request.Own_name || x.players[1]?.Name == request.Own_name) != -1)
					{
						await new ServerPacket.join(new() { Result = new Result.failure(new() { Result = "Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
					}
					else
					{
						int index = waitingList.FindIndex(x => x.players[0]?.Name == request.Opp_name || x.players[1]?.Name == request.Opp_name);
						if(index == -1)
						{
							await new ServerPacket.join(new() { Result = new Result.failure(new() { Result = "No player with that name hosts a game right now" }) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
						}
						else
						{
							string id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(seed + request.Own_name))).Replace("-", "");
							int playerIndex = waitingList[index].players[0] == null ? 0 : 1;
							waitingList[index].players[playerIndex] = new Room.Player(Name: request.Own_name, id: id, client: client, ready: false, noshuffle: false);
							await new ServerPacket.join(new() { Result = new Result.success(new()) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
							if(waitingList[index].players[1 - playerIndex]!.client != null && waitingList[index].players[1 - playerIndex]!.client.Connected)
							{
								try
								{
									await new ServerPacket.opponent_changed(new() { Name = request.Own_name }).WriteAsync(new TCompactProtocol(new TSocketTransport(waitingList[index].players[1 - playerIndex]!.client, config: new())), default);
								}
								catch(Exception e)
								{
									Functions.Log($"Could not send opponent_changed to opponent: {e.Message}");
								}
							}
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case ClientPacket.rooms:
			{
				if(waitingList.Exists(x => x.players[0]?.Name == null && x.players[1]?.Name == null))
				{
					Functions.Log($"There is a player whose name is null", severity: Functions.LogSeverity.Error, includeFullPath: true);
					return HandlePacketReturn.Continue;
				}
				Functions.Log("Before sending rooms list");
				await new ServerPacket.rooms(new() { Rooms = waitingList.FindAll(x => !Array.TrueForAll(x.players, y => y?.ready ?? false)).ConvertAll(x => x.players[0]?.Name ?? x.players[1]?.Name ?? "") }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
				Functions.Log("After sending rooms list");
			}
			break;
			case ClientPacket.additional_cards:
			{
				string fullAdditionalCardsPath = Path.Combine(baseDir, config.additional_cards_path);
				bool isUpToDate = File.Exists(fullAdditionalCardsPath);
				if(isUpToDate)
				{
					ServerAdditionalCards additionalCards = new();
					await additionalCards.ReadAsync(new TCompactProtocol(new Functions.TSimpleFileTransport(fullAdditionalCardsPath, Functions.TSimpleFileTransport.OpenMode.Read)), default);
					if(additionalCards.Timestamp > lastAdditionalCardsTimestamp)
					{
						isUpToDate = false;
					}
				}
				if(!isUpToDate)
				{
					ProcessStartInfo info = new()
					{
						Arguments = config.core_info.Arguments + " --additional_cards_path=" + fullAdditionalCardsPath,
						CreateNoWindow = config.core_info.CreateNoWindow,
						UseShellExecute = config.core_info.UseShellExecute,
						FileName = config.core_info.FileName,
						WorkingDirectory = config.core_info.WorkingDirectory,
					};
					await Process.Start(info)!.WaitForExitAsync();
				}
				if(File.Exists(fullAdditionalCardsPath))
				{
					ServerAdditionalCards additionalCards = new();
					await additionalCards.ReadAsync(new TCompactProtocol(new Functions.TSimpleFileTransport(fullAdditionalCardsPath, Functions.TSimpleFileTransport.OpenMode.Read)), default);
					lastAdditionalCardsTimestamp = additionalCards.Timestamp;
					await new ServerPacket.additional_cards(additionalCards).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
				}
				else
				{
					Functions.Log("No additional cards file exists", severity: Functions.LogSeverity.Warning);
					await new ServerPacket.additional_cards(new()).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
				}
			}
			break;
			case ClientPacket.artworks:
			{
				ClientArtworks request = packet.As_artworks!;
				if(config.artwork_path is null)
				{
					await new ServerPacket.artworks(new() { Supports_artworks = false }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
					break;
				}
				Dictionary<string, ArtworkInfo> artworks = [];
				foreach(string name in request.Names ?? [])
				{
					string sanitizedName = Functions.CardnameToFilename(name);
					string pngPath = Path.Combine(config.artwork_path, sanitizedName + ".png");
					string jpgPath = Path.Combine(config.artwork_path, sanitizedName + ".jpg");
					if(File.Exists(pngPath))
					{
						artworks[sanitizedName] = new() { Filetype = ArtworkFiletype.PNG, Data = await File.ReadAllBytesAsync(pngPath) };
					}
					else if(File.Exists(jpgPath))
					{
						artworks[sanitizedName] = new() { Filetype = ArtworkFiletype.JPG, Data = await File.ReadAllBytesAsync(jpgPath) };
					}
				}
				await new ServerPacket.artworks(new() { Supports_artworks = true, Artworks = artworks }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
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
