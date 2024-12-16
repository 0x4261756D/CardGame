namespace CardGameUtils;

internal class URL(string address, int port)
{
	public string address = address;
	public int port = port;
}

internal class PlatformCoreConfig
{
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
		public PlayerConfig[] players = players;
		public bool noshuffle = noshuffle;
	}
	internal class PlayerConfig(string name, string[] decklist, string id)
	{
		public string name = name;
		public string[] decklist = decklist;
		public string id = id;
	}
	internal class DeckConfig(URL additional_cards_url, string deck_location, bool should_fetch_additional_cards)
	{
		public string deck_location = deck_location;
		public bool should_fetch_additional_cards = should_fetch_additional_cards;
		public URL additional_cards_url = additional_cards_url;
	}

	public int port = port;
	public CoreMode mode = mode;
	public DuelConfig? duel_config = duel_config;
	public DeckConfig? deck_config = deck_config;
}

internal class PlatformClientConfig
{
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

	public URL deck_edit_url = deck_edit_url;
	public int width = width, height = height;
	public bool should_spawn_core = should_spawn_core;
	public CoreInfo core_info = core_info;
	public string? player_name;
	public bool should_save_player_name = should_save_player_name;
	public string server_address = server_address;
	public string? last_deck_name;
	public int animation_delay_in_ms = animation_delay_in_ms;
	public ThemeVariant? theme = theme;
	public string? artwork_path = artwork_path;
}
internal struct CoreInfo
{
	public string FileName;
	public string Arguments;
	public bool CreateNoWindow;
	public string Domain;
	public bool ErrorDialog;
	public bool UseShellExecute;
	public string WorkingDirectory;
}

internal struct PlatformServerConfig
{
	public ServerConfig windows, linux;
}
internal class ServerConfig(string additional_cards_path, string? artwork_path, int port, int room_min_port, int room_max_port, CoreInfo core_info)
{
	public CoreInfo core_info = core_info;
	public int port = port;
	public int room_min_port = room_min_port, room_max_port = room_max_port;
	public string additional_cards_path = additional_cards_path;
	public string? artwork_path = artwork_path;
}
