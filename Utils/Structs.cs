using System;
using System.Text;

namespace CardGameUtils.Structs;

public class CardStruct
{
    public string name, text;
    public GameConstants.CardType card_type;
    public GameConstants.PlayerClass card_class;
    public GameConstants.Location location;
    public int uid, life, power, cost, base_life, base_power, base_cost, position, controller, base_controller;
    public bool is_class_ability;
    public bool can_be_class_ability;
    public CardStruct(string name,
        string text,
        GameConstants.CardType card_type,
        GameConstants.PlayerClass card_class,
        int uid, int life, int power, int cost,
        int base_life, int base_power, int base_cost,
        GameConstants.Location location, int position,
        bool is_class_ability,
        bool can_be_class_ability,
        int controller,
        int base_controller)
    {
        this.name = name;
        this.text = text;
        this.card_type = card_type;
        this.card_class = card_class;
        this.uid = uid;
        this.life = life;
        this.power = power;
        this.cost = cost;
        this.base_life = base_life;
        this.base_power = base_power;
        this.base_cost = base_cost;
        this.location = location;
        this.position = position;
        this.is_class_ability = is_class_ability;
        this.can_be_class_ability = can_be_class_ability;
        this.controller = controller;
        this.base_controller = base_controller;
    }
    public CardStruct()
    {
        name = "UNKNOWN";
        text = "UNKNOWN";
    }

    public override string ToString()
    {
        return Format(separator: '|');
    }

    public override bool Equals(object? other)
    {
        if (other == null)
        {
            return false;
        }
        return uid == ((CardStruct)other).uid;
    }

    public override int GetHashCode()
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }

    public string Format(bool inDeckEdit = false, char separator = '\n')
    {
        StringBuilder builder = new();
        if (!inDeckEdit)
        {
            _ = builder.Append($"UID: {uid}{separator}");
        }
        _ = builder.Append($"name: {name}{separator}");
        if (card_type == GameConstants.CardType.Quest)
        {
            _ = builder.Append($"{separator}quest progress: {position}/{cost}");
        }
        else if (location == GameConstants.Location.Ability)
        {
            _ = builder.Append($"{separator}cost: 1");
        }
        else
        {
            _ = builder.Append($"{separator}cost: {cost}");
            if (!inDeckEdit)
            {
                _ = builder.Append($"/{base_cost}");
            }
        }
        if (!inDeckEdit)
        {
            _ = builder.Append($"{separator}controller: {controller}/{base_controller}");
        }
        _ = builder.Append($"{separator}card_type: {card_type}").Append($"{separator}class: {card_class}");
        if (!inDeckEdit)
        {
            _ = builder.Append($"{separator}location: {location}");
        }
        if (card_type == GameConstants.CardType.Creature)
        {
            _ = builder.AppendJoin(separator, $"{separator}power: {power}{(inDeckEdit ? "" : "/" + base_power)}", $"life: {life}{(inDeckEdit ? "" : "/" + base_life)}");
            if (location == GameConstants.Location.Field)
            {
                _ = builder.Append($"{separator}position: {position}");
            }
        }
        else if (card_type == GameConstants.CardType.Spell && inDeckEdit)
        {
            _ = builder.Append($"{separator}can_be_class_ability: {can_be_class_ability}");
        }
        return builder.Append($"{separator}----------{separator}{text}").ToString();
    }
}


public class URL(string address, int port)
{
    public string address = address;
    public int port = port;
}

public class PlatformCoreConfig
{
    public CoreConfig? windows, linux;
}

public class CoreConfig(int port, CoreConfig.CoreMode mode, CoreConfig.DuelConfig? duel_config = null, CoreConfig.DeckConfig? deck_config = null)
{
    public enum CoreMode
    {
        Duel,
        Client,
    }
    public class DuelConfig(PlayerConfig[] players, bool noshuffle)
    {
        public PlayerConfig[] players = players;
        public bool noshuffle = noshuffle;
    }
    public class PlayerConfig(string name, string[] decklist, string id)
    {
        public string name = name;
        public string[] decklist = decklist;
        public string id = id;
    }
    public class DeckConfig(URL additional_cards_url, string deck_location, bool should_fetch_additional_cards)
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

public class PlatformClientConfig
{
    public ClientConfig? windows, linux;
}

public class CardAction(int uid, string description)
{
    public int uid = uid;
    public string description = description;
}

public class ClientConfig(
    URL deck_edit_url, int width, int height, CoreInfo core_info, bool should_save_player_name, bool should_spawn_core, string server_address, int animation_delay_in_ms, ClientConfig.ThemeVariant? theme, string? picture_path)
{
    public enum ThemeVariant
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
    public string? picture_path = picture_path;
}
public struct CoreInfo
{
    public string FileName;
    public string Arguments;
    public bool CreateNoWindow;
    public string Domain;
    public bool ErrorDialog;
    public bool UseShellExecute;
    public string WorkingDirectory;
}

public struct PlatformServerConfig
{
    public ServerConfig windows, linux;
}
public class ServerConfig(string additional_cards_path, int port, int room_min_port, int room_max_port, CoreInfo core_info)
{
    public CoreInfo core_info = core_info;
    public int port = port;
    public int room_min_port = room_min_port, room_max_port = room_max_port;
    public string additional_cards_path = additional_cards_path;
}

public class NetworkingStructs
{
    // NOTE: The packet class exists for reference only,
    // 		 in practive it is unnecessary to actually serialize a packet,
    // 		 accessing it's type and then parsing it's content is enough.
    public class Packet
    {
        public int length;
        public byte type;
        public PacketContent content = new();
    }
    public class PacketContent { }
    public class DuelPackets
    {
        public class SurrenderRequest : PacketContent
        {
        }

        public class GameResultResponse(GameConstants.GameResult result) : PacketContent
        {
            public GameConstants.GameResult result = result;
        }

        public class GetOptionsRequest(GameConstants.Location location, int uid) : PacketContent
        {
            public GameConstants.Location location = location;
            public int uid = uid;
        }
        public class GetOptionsResponse(GameConstants.Location location, int uid, CardAction[] options) : PacketContent
        {
            public GameConstants.Location location = location;
            public int uid = uid;
            public CardAction[] options = options;
        }

        public class SelectOptionRequest(GameConstants.Location location, int uid, CardAction cardAction) : PacketContent
        {
            public GameConstants.Location location = location;
            public int uid = uid;
            public CardAction cardAction = cardAction;
        }

        public class YesNoRequest(string question) : PacketContent
        {
            public string question = question;
        }
        public class YesNoResponse(bool result) : PacketContent
        {
            public bool result = result;
        }

        public class SelectCardsRequest(CardStruct[] cards, string desc, int amount) : PacketContent
        {
            public CardStruct[] cards = cards;
            public string desc = desc;
            public int amount = amount;
        }
        public class SelectCardsResponse(int[] uids) : PacketContent
        {
            public int[] uids = uids;
        }

        public class CustomSelectCardsRequest(CardStruct[] cards, string desc, bool initialState) : PacketContent
        {
            public CardStruct[] cards = cards;
            public string desc = desc;
            public bool initialState = initialState;
        }
        public class CustomSelectCardsResponse(int[] uids) : PacketContent
        {
            public int[] uids = uids;
        }

        public class CustomSelectCardsIntermediateRequest(int[] uids) : PacketContent
        {
            public int[] uids = uids;
        }
        public class CustomSelectCardsIntermediateResponse(bool isValid) : PacketContent
        {
            public bool isValid = isValid;
        }

        public class FieldUpdateRequest(
            FieldUpdateRequest.Field ownField,
            FieldUpdateRequest.Field oppField,
            int turn,
            bool hasInitiative,
            bool battleDirectionLeftToRight,
            int? markedZone) : PacketContent
        {
            public class Field(
                int life,
                int deckSize,
                int graveSize,
                int momentum,
                CardStruct[] hand,
                CardStruct?[] field,
                string name,
                CardStruct ability,
                CardStruct quest,
                Field.ShownInfo shownInfo)
            {
                public int life = life, deckSize = deckSize, graveSize = graveSize, momentum = momentum;
                public CardStruct[] hand = hand;
                public CardStruct?[] field = field;
                // TODO: Don't always send these
                public string name = name;
                public CardStruct ability = ability, quest = quest;
                public ShownInfo shownInfo = shownInfo;
                public class ShownInfo
                {
                    public CardStruct? card;
                    public string? description;
                }
            }


            public Field ownField = ownField, oppField = oppField;
            public int turn = turn;
            public bool hasInitiative = hasInitiative, battleDirectionLeftToRight = battleDirectionLeftToRight;
            public int? markedZone = markedZone;
        }

        public class SelectZoneRequest(bool[] options) : PacketContent
        {
            public bool[] options = options;
        }
        public class SelectZoneResponse(int zone) : PacketContent
        {
            public int zone = zone;
        }

        internal class PassRequest : PacketContent
        {
        }

        internal class ViewGraveRequest(bool opponent) : PacketContent
        {
            public bool opponent = opponent;
        }
        internal class ViewCardsResponse(string message, CardStruct[] cards) : PacketContent
        {
            public string message = message;
            public CardStruct[] cards = cards;
        }
    }

    public class DeckPackets
    {
        public struct Deck
        {
            public string name;
            public CardStruct[] cards;
            public GameConstants.PlayerClass player_class;
            public CardStruct? ability, quest;
            public override readonly string? ToString()
            {
                if (name == null)
                {
                    return null;
                }
                StringBuilder builder = new();
                _ = builder.Append(player_class);
                if (ability != null)
                {
                    _ = builder.Append("\n#").Append(ability.name);
                }
                if (quest != null)
                {
                    _ = builder.Append("\n|").Append(quest.name);
                }
                foreach (var card in cards)
                {
                    _ = builder.Append('\n').Append(card.name);
                }
                return builder.ToString();
            }
        }

        public class NamesRequest : PacketContent
        {
        }

        public class NamesResponse(string[] names) : PacketContent
        {
            public string[] names = names;
        }

        public class ListRequest(string name) : PacketContent
        {
            public string name = name;
        }
        public class ListResponse(Deck deck) : PacketContent
        {
            public Deck deck = deck;
        }

        public class SearchRequest(string filter, GameConstants.PlayerClass playerClass, bool includeGenericCards) : PacketContent
        {
            public string filter = filter;
            public GameConstants.PlayerClass playerClass = playerClass;
            public bool includeGenericCards = includeGenericCards;
        }
        public class SearchResponse(CardStruct[] cards) : PacketContent
        {
            public CardStruct[] cards = cards;
        }

        public class ListUpdateRequest(Deck deck) : PacketContent
        {
            public Deck deck = deck;
        }
        public class ListUpdateResponse(bool shouldUpdate) : PacketContent
        {
            public bool shouldUpdate = shouldUpdate;
        }
    }

    public class ServerPackets
    {
        public class AdditionalCardsRequest : PacketContent
        {
        }
        public class AdditionalCardsResponse(DateTime time, CardStruct[] cards) : PacketContent
        {
            public DateTime time = time;
            public CardStruct[] cards = cards;
        }

        public class CreateRequest(string name) : PacketContent
        {
            public string name = name;
        }
        public class CreateResponse(bool success, string? reason = null) : PacketContent
        {
            public bool success = success;
            public string? reason = reason;
        }

        public class JoinRequest(string name, string targetName) : PacketContent
        {
            public string name = name, targetName = targetName;
        }
        public class JoinResponse(bool success, string? reason = null) : PacketContent
        {
            public bool success = success;
            public string? reason = reason;
        }

        public class OpponentChangedResponse(string? name) : PacketContent
        {
            public string? name = name;
        }

        public class LeaveRequest() : PacketContent { }

        public class RoomsRequest : PacketContent
        {

        }
        public class RoomsResponse(string[] rooms) : PacketContent
        {
            public string[] rooms = rooms;
        }

        public class StartRequest(string[] decklist, bool noshuffle) : PacketContent
        {
            public string[] decklist = decklist;
            public bool noshuffle = noshuffle;
        }
        public class StartResponse : PacketContent
        {
            public enum Result
            {
                Failure,
                Success,
                SuccessButWaiting,
            }
            public Result success;
            public string? id;
            public int port;
            public string? reason;
        }
    }
}
