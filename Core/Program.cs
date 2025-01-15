using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using CardGameUtils;
using CardGameUtils.Replay;
using CardGameUtils.Structs.Server;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using CardGameUtils.Base;
using static CardGameUtils.Functions;
namespace CardGameCore;

class Program
{
	public static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
	public static Replay? replay;
	public static int seed;
	public static ulong versionTime;
	public static void Main(string[] args)
	{
		seed = new Random().Next();
		string? configPath = null;
		versionTime = GenerateVersionTime();
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
						GenerateAdditionalCards(path);
						Log($"Done generating new additional cards referring to {versionTime}");
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
		PlatformCoreConfig? platformConfig = JsonSerializer.Deserialize<PlatformCoreConfig>(File.ReadAllText(Path.GetFullPath(configPath)), GenericConstants.platformCoreConfigSerialization);
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
						replay = new Replay(cmdline_args: [.. args], seed: seed, packets: []);
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
		core.Init(pipeStream);
		Log("EXITING");
		if(replay is not null)
		{
			string replayPath = Path.Combine(baseDir, "replays");
			_ = Directory.CreateDirectory(replayPath);
			string filePath = Path.Combine(replayPath, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{UsernameToFilename(config.duel_config!.players[0].name)}_vs_{UsernameToFilename(config.duel_config!.players[1].name)}.replay");
			File.WriteAllBytes(filePath, replay.Serialize());
			Log("Wrote replay to " + filePath);
		}
	}

	private static ulong GenerateVersionTime()
	{
		foreach(string file in Directory.EnumerateFiles(baseDir))
		{
			if(Path.GetFileName(file) is "CardGameCore.dll" or "CardGameCore" or "CardGameCore.exe")
			{
				return (ulong)(new DateTimeOffset(File.GetCreationTime(file)).ToUnixTimeSeconds());
			}
		}
		throw new Exception($"Could not find executable in {baseDir} to generate version time");
	}

	// NOTE: This can easily be a bottleneck since it does some not-so-cheap operations.
	public static void GenerateAdditionalCards(string path)
	{
		List<CardStruct> cards = [.. Array.ConvertAll(Array.FindAll(Assembly.GetExecutingAssembly().GetTypes(), IsCardSubclass), x => ((Card)Activator.CreateInstance(x)!).ToStruct())];
		if(File.Exists(path))
		{
			IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
			foreach(CardStruct card in cards)
			{
				hasher.AppendData(card.Serialize());
			}
			byte[] newHash = hasher.GetHashAndReset();
			List<CardStruct> oldCards = ((SToC_Response_AdditionalCards)SToC_Packet.Deserialize(File.ReadAllBytes(path)).content).cards;
			foreach(CardStruct card in oldCards)
			{
				hasher.AppendData(card.Serialize());
			}
			byte[] oldHash = hasher.GetHashAndReset();
			if(oldHash.AsSpan().SequenceEqual(newHash))
			{
				// There was no change since the last request so no new data has to be written.
				return;
			}
		}
		File.WriteAllBytes(path, new SToC_Packet(new SToC_Content.additional_cards(new(cards: cards, timestamp: (ulong)DateTimeOffset.Now.ToUnixTimeSeconds()))).Serialize());
	}

	public static readonly Predicate<Type> IsCardSubclass = card =>
	{
		return card != typeof(Token) && (card.BaseType == typeof(Spell) || card.BaseType == typeof(Creature) || card.BaseType == typeof(Quest));
	};
}
