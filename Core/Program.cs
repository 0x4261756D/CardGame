using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Reflection;
using CardGameUtils;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using Thrift.Protocol;
using CardGameUtils.Packets.Server;
using System.Threading.Tasks;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Duel;

namespace CardGameCore;

class Program
{
	public static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
	public static Replay? replay;
	public static int seed;
	public static long versionTimestamp;

	public static async Task Main(string[] args)
	{
		seed = new Random().Next();
		string? configPath = null;
		versionTimestamp = GenerateVersionTimestamp();
		for(int i = 0; i < args.Length; i++)
		{
			string[] parts = args[i].Split('=');
			if(parts.Length == 2)
			{
				string path = Path.Combine(baseDir, parts[1]);
				switch(parts[0])
				{
					case "--config":
						if(File.Exists(path))
						{
							configPath = path;
						}
						else
						{
							Log($"No config file found at {Path.GetFullPath(path)}.", severity: LogSeverity.Error);
							return;
						}
						break;
					case "--additional_cards_path":
						await GenerateAdditionalCards(path);
						Log($"Done generating new additional cards referring to {versionTimestamp}");
						return;
				}
			}
		}
		if(configPath == null)
		{
			Log("Please supply a config location with '--config=<path/to/config.json>'");
			return;
		}
		if(!File.Exists(configPath))
		{
			Log($"Missing a config at {configPath}.", severity: LogSeverity.Error);
			return;
		}
		using FileStream stream = File.OpenRead(Path.GetFullPath(configPath));
		PlatformCoreConfig? platformConfig = await JsonSerializer.DeserializeAsync<PlatformCoreConfig>(stream, GenericConstants.platformCoreConfigSerialization);
		if(platformConfig == null)
		{
			Log("Could not parse a platform config", LogSeverity.Error);
			return;
		}
		CoreConfig config;
		if(Environment.OSVersion.Platform == PlatformID.Unix)
		{
			config = platformConfig.linux!;
		}
		else
		{
			config = platformConfig.windows!;
		}
		bool modeSet = false;
		PipeStream? pipeStream = null;
		foreach(string s in args)
		{
			if(s.StartsWith("--"))
			{
				string arg = s[2..].Split('=')[0];
				string parameter = s[(arg.Length + 3)..];
				switch(arg)
				{
					case "mode":
						if(parameter == "duel")
						{
							config.mode = CoreConfig.CoreMode.Duel;
						}
						else
						{
							config.mode = CoreConfig.CoreMode.Client;
						}
						modeSet = true;
						break;
					case "players":
						CoreConfig.PlayerConfig[] players = JsonSerializer.Deserialize<CoreConfig.PlayerConfig[]>(Encoding.UTF8.GetString(Convert.FromBase64String(parameter)), options: GenericConstants.platformCoreConfigSerialization)!;
						if(config.duel_config == null)
						{
							config.duel_config = new CoreConfig.DuelConfig(players: players, noshuffle: false);
						}
						else
						{
							config.duel_config.players = players;
						}
						break;
					case "noshuffle":
						if(config.duel_config != null)
						{
							config.duel_config.noshuffle = Convert.ToBoolean(parameter);
						}
						break;
					case "port":
						config.port = Convert.ToInt32(parameter);
						break;
					case "config":
						break;
					case "replay":
						Log("Recording replay");
						replay = new Replay
						{
							Seed = seed,
							Cmdline_args = [.. args],
						};
						break;
					case "additional_cards_url":
						if(config.deck_config != null)
						{
							config.deck_config.additional_cards_url = new URL(parameter, 7043);
						}
						break;
					case "seed":
						seed = Convert.ToInt32(parameter);
						break;
					case "pipe":
					{
						pipeStream = new AnonymousPipeClientStream(PipeDirection.Out, parameter);
					}
					break;
					default:
						Log($"Unknown argument {s} ({arg}, {parameter})", severity: LogSeverity.Error);
						return;
				}
			}
		}
		if(!modeSet)
		{
			Log("No mode supplied, please do so with --mode={client|duel|test}");
			return;
		}
		Core core;
		if(config.mode == CoreConfig.CoreMode.Client)
		{
			core = new ClientCore(config.deck_config!, config.port);
		}
		else
		{
			core = new DuelCore(config.duel_config!, config.port);
		}
		await core.Init(pipeStream);
		Log("EXITING");
		if(replay != null)
		{
			string replayPath = Path.Combine(baseDir, "replays");
			_ = Directory.CreateDirectory(replayPath);
			string filePath = Path.Combine(replayPath, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{UsernameToFilename(config.duel_config!.players[0].name)}_vs_{UsernameToFilename(config.duel_config!.players[1].name)}.replay");
			// TODO: Check if this actually works
			await replay.WriteAsync(new TJsonProtocol(new Functions.TSimpleFileTransport(filePath, Functions.TSimpleFileTransport.OpenMode.Write)), default);
			Log("Wrote replay to " + filePath);
		}
	}

	private static long GenerateVersionTimestamp()
	{
		foreach(string file in Directory.EnumerateFiles(baseDir))
		{
			if(Path.GetFileName(file) is "CardGameCore.dll" or "CardGameCore" or "CardGameCore.exe")
			{
				return new DateTimeOffset(File.GetCreationTime(file)).ToUnixTimeSeconds();
			}
		}
		throw new Exception($"Could not find executable in {baseDir} to generate version time");
	}

	public static async Task GenerateAdditionalCards(string path)
	{
		if(File.Exists(path))
		{
			ServerAdditionalCards additional = new();
			await additional.ReadAsync(new TCompactProtocol(new TSimpleFileTransport(path, TSimpleFileTransport.OpenMode.Read)), default);
			if(additional.Timestamp >= versionTimestamp)
			{
				return;
			}
		}
		Log("Generating new additional cards");
		List<CardInfo> cards = [];
		foreach(Type card in Array.FindAll(Assembly.GetExecutingAssembly().GetTypes(), IsCardSubclass))
		{
			Card c = (Card)Activator.CreateInstance(card)!;
			cards.Add(c.ToStruct(client: true));
		}
		await new ServerAdditionalCards()
		{
			Cards = [.. cards],
			Timestamp = versionTimestamp,
		}.WriteAsync(new TCompactProtocol(new TSimpleFileTransport(path, TSimpleFileTransport.OpenMode.Write)), default);
	}

	public static readonly Predicate<Type> IsCardSubclass = card =>
	{
		return card != typeof(Token) && (card.BaseType == typeof(Spell) || card.BaseType == typeof(Creature) || card.BaseType == typeof(Quest));
	};
}
