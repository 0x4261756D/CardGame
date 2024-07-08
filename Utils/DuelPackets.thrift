include "Constants.thrift"
namespace netstd CardGame.Packets.Duel

union ClientPacket
{
	1: ClientSurrender surrender;
	2: ClientGetOptions get_options;
	3: ClientSelectOption select_option;
	4: ClientYesNo yes_no;
	5: ClientSelectCards select_cards;
	6: ClientCustomSelectCards custom_select_cards;
	7: ClientCustomSelectCardsIntermediate custom_select_cards_intermediate;
}

struct ClientSurrender {}
struct ClientGetOptions
{
	1: Constants.Location location;
	2: i32 uid;
}
struct ClientSelectOption
{
	1: Constants.Location location;
	2: i32 uid;
	3: CardAction action;
}
struct ClientYesNo
{
	1: bool yes;
}
struct ClientSelectCards
{
	1: list<i32> uids;
}
struct ClientCustomSelectCards
{
	1: list<i32> uids;
}
struct ClientCustomSelectCardsIntermediate
{
	1: list<i32> uids;
}

union ServerPacket
{
	1: ServerGameResult game_result;
	2: ServerGetOptions get_options;
	3: ServerYesNo yes_no;
	4: ServerSelectCards select_cards;
	5: ServerCustomSelectCards custom_select_cards;
	6: ServerCustomSelectCardsIntermediate custom_select_cards_intermediate;
	7: ServerFieldUpdate field_update;
}

struct ServerGameResult
{
	1: Constants.GameResult result;
}
struct ServerGetOptions
{
	1: Constants.Location location;
	2: i32 uid;
	3: list<CardAction> options;
}
struct ServerYesNo
{
	1: string question;
}
struct ServerSelectCards
{
	1: list<Constants.CardInfo> cards;
	2: string description;
	3: i32 amount;
}
struct ServerCustomSelectCards
{
	1: list<Constants.CardInfo> cards;
	2: string description;
	3: bool initial_state;
}
struct ServerCustomSelectCardsIntermediate
{
	1: bool is_valid;
}
struct ServerFieldUpdate
{
	1: FieldUpdateField own;
	2: FieldUpdateField opp;
	3: i32 turn;
	4: bool has_initiative;
	5: bool is_battle_direction_left_to_right;
	6: optional i32 marked_zone;
}
struct FieldUpdateField
{
	1: i32 life;
	2: i32 deck_size;
	3: i32 grave_size;
	4: i32 momentum;
	5: list<Constants.CardInfo> hand;
	6: list<FieldCardInfo> field;
	7: string name;
	8: Constants.CardInfo ability;
	9: Constants.CardInfo quest;
	10: ShownInfo shown_info;
}
struct FieldCardInfo
{
	1: optional Constants.CardInfo info;
}
struct ShownInfo
{
	1: optional Constants.CardInfo card;
	2: optional string description;
}

struct CardAction
{
	1: i32 uid;
	2: string description;
}

