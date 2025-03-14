using System.Text.Json.Serialization;

namespace CardGameUtils;

internal class URL(string address, int port)
{
	[JsonInclude]
	public string address = address;
	[JsonInclude]
	public int port = port;
}

internal class PlatformCoreConfig
{
	[JsonInclude]
	public CoreConfig? windows, linux;
}

internal class CoreConfig(int port, CoreConfig.CoreMode mode, CoreConfig.DuelConfig? duel_config = null, CoreConfig.DeckConfig? deck_config = null)
{
	internal enum CoreMode
	{
		Duel,
		Client,
	}
	internal class DuelConfig(PlayerConfig[] players, bool noshuffle)
	{
		[JsonInclude]
		public PlayerConfig[] players = players;
		[JsonInclude]
		public bool noshuffle = noshuffle;
	}
	internal class PlayerConfig(string name, string[] decklist, string id)
	{
		[JsonInclude]
		public string name = name;
		[JsonInclude]
		public string[] decklist = decklist;
		[JsonInclude]
		public string id = id;
	}
	internal class DeckConfig(URL additional_cards_url, string deck_location, bool should_fetch_additional_cards)
	{
		[JsonInclude]
		public string deck_location = deck_location;
		[JsonInclude]
		public bool should_fetch_additional_cards = should_fetch_additional_cards;
		[JsonInclude]
		public URL additional_cards_url = additional_cards_url;
	}

	[JsonInclude]
	public int port = port;
	[JsonInclude]
	public CoreMode mode = mode;
	[JsonInclude]
	public DuelConfig? duel_config = duel_config;
	[JsonInclude]
	public DeckConfig? deck_config = deck_config;
}

internal class PlatformClientConfig
{
	[JsonInclude]
	public ClientConfig? windows, linux;
}

internal class ClientConfig(
	URL deck_edit_url, int width, int height, CoreInfo core_info, bool should_save_player_name, bool should_spawn_core, string server_address, int animation_delay_in_ms, ClientConfig.ThemeVariant? theme, string? artwork_path)
{
	internal enum ThemeVariant
	{
		Default,
		Dark,
		Light,
	}

	[JsonInclude]
	public URL deck_edit_url = deck_edit_url;
	[JsonInclude]
	public int width = width, height = height;
	[JsonInclude]
	public bool should_spawn_core = should_spawn_core;
	[JsonInclude]
	public CoreInfo core_info = core_info;
	[JsonInclude]
	public string? player_name;
	[JsonInclude]
	public bool should_save_player_name = should_save_player_name;
	[JsonInclude]
	public string server_address = server_address;
	[JsonInclude]
	public string? last_deck_name;
	[JsonInclude]
	public int animation_delay_in_ms = animation_delay_in_ms;
	[JsonInclude]
	public ThemeVariant? theme = theme;
	[JsonInclude]
	public string? artwork_path = artwork_path;
}
internal struct CoreInfo
{
	[JsonInclude]
	public string FileName;
	[JsonInclude]
	public string Arguments;
	[JsonInclude]
	public bool CreateNoWindow;
	[JsonInclude]
	public string Domain;
	[JsonInclude]
	public bool ErrorDialog;
	[JsonInclude]
	public bool UseShellExecute;
	[JsonInclude]
	public string WorkingDirectory;
}

internal struct PlatformServerConfig
{
	[JsonInclude]
	public ServerConfig windows, linux;
}
internal class ServerConfig(string additional_cards_path, string? artwork_path, int port, int room_min_port, int room_max_port, CoreInfo core_info)
{
	[JsonInclude]
	public CoreInfo core_info = core_info;
	[JsonInclude]
	public int port = port;
	[JsonInclude]
	public int room_min_port = room_min_port, room_max_port = room_max_port;
	[JsonInclude]
	public string additional_cards_path = additional_cards_path;
	[JsonInclude]
	public string? artwork_path = artwork_path;
}
