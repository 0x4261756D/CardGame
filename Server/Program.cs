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
using CardGameUtils.Structs;
using CardGameUtils.Packets.Server;
using CardGameUtils.Constants;
using Google.FlatBuffers;

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
		PlatformServerConfig platformConfig = JsonSerializer.Deserialize<PlatformServerConfig>(File.ReadAllText(configLocation), InternalConstants.platformServerConfigSerialization);
		if(Environment.OSVersion.Platform == PlatformID.Unix)
		{
			config = platformConfig.linux;
		}
		else
		{
			config = platformConfig.windows;
		}
		if(File.Exists(Path.Combine(baseDir, config.additional_cards_path)))
		{
			lastAdditionalCardsTimestamp = ServerAdditionalCardsPacket.GetRootAsServerAdditionalCardsPacket(new ByteBuffer(File.ReadAllBytes(Path.Combine(baseDir, config.additional_cards_path)))).Timestamp;
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
					ClientPacket packet = Functions.ReadSizedServerClientPacketFromStream(stream);
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
					ClientPacket packet = Functions.ReadSizedServerClientPacketFromStream(player.stream);
					switch(packet.ContentType)
					{
						case ClientContent.leave:
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
										room.players[1 - playerIndex]?.stream.Write(ServerPacketTToByteArray(new(){Content=new(){Type=ServerContent.opponent_changed, Value = new ServerOpponentChangedPacketT()}}));
									}
									catch(IOException e)
									{
										Functions.Log($"Could not send OpponentChangedResponse: {e.Message}");
									}
								}
							}
						}
						break;
						case ClientContent.start:
						{
							ClientStartPacketT request = packet.ContentAsstart().UnPack();
							for(int i = request.Decklist.Count - 1; i >= 0; i--)
							{
								if(string.IsNullOrWhiteSpace(request.Decklist[i]))
								{
									request.Decklist.RemoveAt(i);
								}
							}
							Functions.Log("----START REQUEST HANDLING----", includeFullPath: true);
							Functions.Log($"rdc {request.Decklist.Count} vs. {GameConstants.DECK_SIZE + 3}");
							if(request.Decklist.Count != GameConstants.DECK_SIZE + 3)
							{
								player.stream.Write(ServerPacketTToByteArray(new()
								{
									Content = new()
									{
										Type = ServerContent.start,
										Value = new ServerStartPacketT
										{
											Result = new()
											{
												Type = ServerStartResult.ServerStartResultFailure,
												Value = new ServerStartResultFailureT
												{
													Reason = "Your deck has the wrong size",
												}
											}
										}
									}
								}));
								break;
							}
							Functions.Log("Player: " + playerIndex, includeFullPath: true);
							player.ready = true;
							player.noshuffle = request.Noshuffle;
							player.Decklist = request.Decklist;
							if(room.players[1 - playerIndex] == null)
							{
								Functions.Log("No opponent", includeFullPath: true);
								player.stream.Write(ServerPacketTToByteArray(new()
								{
									Content = new()
									{
										Type = ServerContent.start,
										Value = new ServerStartPacketT
										{
											Result = new()
											{
												Type = ServerStartResult.ServerStartResultFailure,
												Value = new ServerStartResultFailureT
												{
													Reason = "You have no opponent",
												}
											}
										}
									}
								}));
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
											p.stream.Write(ServerPacketTToByteArray(new()
											{
												Content = new()
												{
													Type = ServerContent.start,
													Value = new ServerStartPacketT
													{
														Result = new()
														{
															Type = ServerStartResult.ServerStartResultSuccessButWaiting,
															Value = new ServerStartResultFailureT()
														}
													}
												}
											}));
										}
									}
								}
								else
								{
									Functions.Log("Could not create the core", severity: Functions.LogSeverity.Error, includeFullPath: true);
									player.stream.Write(ServerPacketTToByteArray(new()
									{
										Content = new()
										{
											Type = ServerContent.start,
											Value = new ServerStartPacketT
											{
												Result = new()
												{
													Type = ServerStartResult.ServerStartResultFailure,
													Value = new ServerStartResultFailureT
													{
														Reason = "Could not create the core",
													}
												}
											}
										}
									}));
								}
							}
							else
							{
								Functions.Log("Opponent not ready", includeFullPath: true);
								player.stream.Write(ServerPacketTToByteArray(new()
								{
									Content = new()
									{
										Type = ServerContent.start,
										Value = new ServerStartPacketT
										{
											Result = new()
											{
												Type = ServerStartResult.ServerStartResultFailure,
												Value = new ServerStartResultFailureT
												{
													Reason = "Your opponent isn't ready yet",
												}
											}
										}
									}
								}));
							}
							Functions.Log("----END----", includeFullPath: true);
						}
						break;
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

	private static HandlePacketReturn HandlePacket(ClientPacket packet, NetworkStream stream)
	{
		CleanupRooms();
		switch(packet.ContentType)
		{
			case ClientContent.create:
			{
				string name = packet.ContentAscreate().Name;
				if(string.IsNullOrWhiteSpace(name))
				{
					stream.Write(ServerPacketTToByteArray(new()
					{
						Content=new()
						{
							Type=ServerContent.create,
							Value = new ServerCreatePacketT()
							{
								Result=new()
								{
									Type = Result.ResultFailure,
									Value = new ResultFailureT()
									{
										Reason = "Names can't be empty"
									}
								}
							}
						}
					}));
				}
				else
				{
					if(waitingList.Exists(x => x.players[0]?.Name == name || x.players[1]?.Name == name))
					{
						stream.Write(ServerPacketTToByteArray(new()
						{
							Content=new()
							{
								Type=ServerContent.create,
								Value = new ServerCreatePacketT()
								{
									Result=new()
									{
										Type = Result.ResultFailure,
										Value = new ResultFailureT()
										{
											Reason = "Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)"
										}
									}
								}
							}
						}));
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
							stream.Write(ServerPacketTToByteArray(new()
							{
								Content=new()
								{
									Type=ServerContent.create,
									Value = new ServerCreatePacketT()
									{
										Result=new()
										{
											Type = Result.ResultFailure,
											Value = new ResultFailureT()
											{
												Reason = "No free port found"
											}
										}
									}
								}
							}));
						}
						else
						{
							waitingList.Add(new Room(name, id, currentPort, stream));
							stream.Write(ServerPacketTToByteArray(new()
							{
								Content=new()
								{
									Type=ServerContent.create,
									Value = new ServerCreatePacketT()
									{
										Result=new()
										{
											Type = Result.ResultSuccess,
											Value = new ResultSuccessT()
										}
									}
								}
							}));
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case ClientContent.join:
			{
				ClientJoinPacket request = packet.ContentAsjoin();
				if(string.IsNullOrWhiteSpace(request.OwnName) || string.IsNullOrWhiteSpace(request.OppName))
				{
					stream.Write(ServerPacketTToByteArray(new()
					{
						Content=new()
						{
							Type=ServerContent.join,
							Value = new ServerJoinPacketT()
							{
								Result=new()
								{
									Type = Result.ResultFailure,
									Value = new ResultFailureT()
									{
										Reason = "Names can't be empty"
									}
								}
							}
						}
					}));
				}
				else
				{
					if(waitingList.FindIndex(x => x.players[0]?.Name == request.OwnName || x.players[1]?.Name == request.OwnName) != -1)
					{
						stream.Write(ServerPacketTToByteArray(new()
						{
							Content=new()
							{
								Type=ServerContent.join,
								Value = new ServerJoinPacketT()
								{
									Result=new()
									{
										Type = Result.ResultFailure,
										Value = new ResultFailureT()
										{
											Reason = "Oh oh, sorry kiddo, looks like someone else already has that name. Why don't you pick something else? (Please watch SAO Abridged if you don't get this reference)"
										}
									}
								}
							}
						}));
					}
					else
					{
						int index = waitingList.FindIndex(x => x.players[0]?.Name == request.OppName || x.players[1]?.Name == request.OppName);
						if(index == -1)
						{
							stream.Write(ServerPacketTToByteArray(new()
							{
								Content=new()
								{
									Type=ServerContent.create,
									Value = new ServerCreatePacketT()
									{
										Result=new()
										{
											Type = Result.ResultFailure,
											Value = new ResultFailureT()
											{
												Reason = "No player with that name hosts a game right now"
											}
										}
									}
								}
							}));
						}
						else
						{
							Functions.Log($"Letting {request.OwnName} join room of {request.OppName}");
							string id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(seed + request.OwnName))).Replace("-", "");
							int playerIndex = waitingList[index].players[0] == null ? 0 : 1;
							waitingList[index].players[playerIndex] = new Room.Player(Name: request.OwnName, id: id, stream: stream, ready: false, noshuffle: false);
							stream.Write(ServerPacketTToByteArray(new()
							{
								Content=new()
								{
									Type=ServerContent.join,
									Value = new ServerJoinPacketT()
									{
										Result=new()
										{
											Type = Result.ResultSuccess,
											Value = new ResultSuccessT()
										}
									}
								}
							}));
							if(waitingList[index].players[1 - playerIndex]!.stream != null && waitingList[index].players[1 - playerIndex]!.stream.Socket.Connected)
							{
								waitingList[index].players[1 - playerIndex]!.stream.Write(ServerPacketTToByteArray(new()
								{
									Content=new()
									{
										Type=ServerContent.opponent_changed,
										Value = new ServerOpponentChangedPacketT()
										{
											Name = request.OwnName
										}
									}
								}));
							}
							return HandlePacketReturn.ContinueKeepStream;
						}
					}
				}
			}
			break;
			case ClientContent.rooms:
			{
				if(waitingList.Exists(x => x.players[0]?.Name == null && x.players[1]?.Name == null))
				{
					Functions.Log($"There is a player whose name is null", severity: Functions.LogSeverity.Error, includeFullPath: true);
					return HandlePacketReturn.Continue;
				}
				stream.Write(ServerPacketTToByteArray(new()
				{
					Content=new()
					{
						Type=ServerContent.rooms,
						Value = new ServerRoomsPacketT()
						{
							Rooms = waitingList.FindAll(x => !Array.TrueForAll(x.players, y => y?.ready ?? false)).ConvertAll(x => x.players[0]?.Name ?? x.players[1]?.Name)
						}
					}
				}));
			}
			break;
			case ClientContent.additional_cards:
			{
				string fullAdditionalCardsPath = Path.Combine(baseDir, config.additional_cards_path);
				if(!File.Exists(fullAdditionalCardsPath) ||
					ServerAdditionalCardsPacket.GetRootAsServerAdditionalCardsPacket(new ByteBuffer(File.ReadAllBytes(fullAdditionalCardsPath))).Timestamp > lastAdditionalCardsTimestamp)
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
					ServerAdditionalCardsPacketT response = ServerAdditionalCardsPacket.GetRootAsServerAdditionalCardsPacket(new ByteBuffer(File.ReadAllBytes(fullAdditionalCardsPath))).UnPack();
					lastAdditionalCardsTimestamp = response.Timestamp;
					stream.Write(ServerPacketTToByteArray(new()
					{
						Content = new()
						{
							Type = ServerContent.additional_cards,
							Value = response
						}
					}));
				}
				else
				{
					Functions.Log("No additional cards file exists", severity: Functions.LogSeverity.Warning);
					stream.Write(ServerPacketTToByteArray(new()
					{
						Content = new()
						{
							Type = ServerContent.additional_cards,
							Value = new ServerAdditionalCardsPacketT()
						}
					}));
				}
			}
			break;
			case ClientContent.artworks:
			{
				if(config.artwork_path is null)
				{
					stream.Write(ServerPacketTToByteArray(new()
					{
						Content = new()
						{
							Type = ServerContent.artworks,
							Value = new ServerArtworksPacketT
							{
								SupportsArtworks = false,
							}
						}
					}));
					break;
				}
				// FIXME:	This crashes when the packet is decoded because the vtable offset (pos) in
				//			https://github.com/google/flatbuffers/blob/fb9afbafc7dfe226b9db54d4923bfb8839635274/net/FlatBuffers/FlatBufferVerify.cs#L243
				//			is too big for a short. Quite likely that this is a fault on the creation side since the flatc tool also rejects it.
				//			No idea why though, there are no structs used and the amount of fields is also small so the vtable should be small as well.
				//
				//			The fix for now is to always send SupportsArtworks = false, unfortunate but no idea how to debug this further right now.
				// ClientArtworksPacket request = packet.ContentAsartworks();
				// ServerArtworksPacketT response = new() { SupportsArtworks = true, Artworks = [] };
				// for(int i = 0; i < request.NamesLength; i++)
				// {
				// 	string sanitizedName = Functions.CardnameToFilename(request.Names(i));
				// 	string pngPath = Path.Combine(config.artwork_path, sanitizedName + ".png");
				// 	string jpgPath = Path.Combine(config.artwork_path, sanitizedName + ".jpg");
				// 	if(File.Exists(pngPath))
				// 	{
				// 		response.Artworks.Add(new(){Name = sanitizedName, Filetype = ArtworkFiletype.PNG, Data = [.. File.ReadAllBytes(pngPath)]});
				// 	}
				// 	else if(File.Exists(jpgPath))
				// 	{
				// 		response.Artworks.Add(new(){Name = sanitizedName, Filetype = ArtworkFiletype.JPG, Data = [.. File.ReadAllBytes(jpgPath)]});
				// 	}
				// }
				// Functions.Log($"Size: {response.Artworks.Count}");
				// byte[] bytes = ServerPacketTToByteArray(new()
				// {
				// 	Content = new()
				// 	{
				// 		Type = ServerContent.artworks,
				// 		Value = response,
				// 	}
				// });
				byte[] bytes = ServerPacketTToByteArray(new(){Content=new(){Type=ServerContent.artworks, Value = new ServerArtworksPacketT{SupportsArtworks=false}}});
				stream.Write(bytes);
			}
			break;
			default:
			{
				throw new Exception($"ERROR: Unable to process this packet: Packet type: {packet.GetType()}");
			}
		}
		return HandlePacketReturn.Continue;
	}


	public static byte[] ServerPacketTToByteArray(ServerPacketT packet)
	{
		FlatBufferBuilder builder = new(1);
		builder.FinishSizePrefixed(ServerPacket.Pack(builder, packet).Value);
		return builder.DataBuffer.ToSizedArray();
	}
}
