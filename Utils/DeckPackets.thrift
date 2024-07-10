include "Constants.thrift"
namespace netstd CardGameUtils.Packets.Deck

union ClientPacket
{
	1: ClientNames names;
	2: ClientList list;
	3: ClientSearch search;
	4: ClientUpdateList update_list;
	5: ClientDeleteList delete_list;
}
struct ClientNames {}
struct ClientList
{
	1: string name;
}
struct ClientSearch
{
	1: string filter;
	2: Constants.PlayerClass player_class;
	3: bool include_generic_cards;
}
struct ClientUpdateList
{
	1: DeckInfo deck;
}
struct ClientDeleteList
{
	1: string name;
}

union ServerPacket
{
	1: ServerNames names;
	2: ServerList list;
	3: ServerSearch search;
}
struct ServerNames
{
	1: list<string> names;
}
struct ServerList
{
	1: optional DeckInfo deck;
}
struct ServerSearch
{
	1: list<Constants.CardInfo> cards;
}

struct DeckInfo
{
	1: string name;
	2: list<Constants.CardInfo> cards;
	3: optional Constants.PlayerClass player_class;
	4: optional Constants.CardInfo ability;
	5: optional Constants.CardInfo quest;
}
