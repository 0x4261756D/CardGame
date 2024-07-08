include "Constants.thrift"
namespace netstd CardGame.Packets.Server

union ClientPacket
{
	1: ClientAdditionalCards additional_cards;
	2: ClientArtworks artworks;
	3: ClientCreate create;
	4: ClientJoin join;
	5: ClientLeave leave;
	6: ClientRooms rooms;
	7: ClientStart start;
}
struct ClientAdditionalCards {}
struct ClientArtworks
{
	1: list<string> names;
}
struct ClientCreate
{
	1: string name;
}
struct ClientJoin
{
	1: string own_name;
	2: string opp_name;
}
struct ClientLeave {}
struct ClientRooms {}
struct ClientStart
{
	1: list<string> decklist;
	2: bool noshuffle;
}

union ServerPacket
{
	1: ServerAdditionalCards additional_cards;
	2: ServerArtworks artworks;
	3: ServerCreate create;
	4: ServerJoin join;
	5: ServerOpponentChanged opponent_changed;
	6: ServerRooms rooms;
	7: ServerStart start;
}
struct ServerAdditionalCards
{
	1: i64 timestamp;
	2: list<Constants.CardInfo> cards;
}
struct ServerArtworks
{
	1: bool supports_artworks;
	2: map<string, ArtworkInfo> artworks;
}
struct ArtworkInfo
{
	1: ArtworkFiletype filetype;
	2: binary data;
}
enum ArtworkFiletype
{
	JPG = 1
	PNG = 2
}
struct ServerCreate
{
	1: Result result;
}
struct ServerJoin
{
	1: Result result;
}
struct ServerOpponentChanged
{
	1: optional string name;
}
struct ServerRooms
{
	1: list<string> rooms;
}
struct ServerStart
{
	1: ServerStartResult result;
}
union ServerStartResult
{
	1: ServerStartResultSuccessButWaiting success_but_waiting;
	2: ResultFailure failure;
	3: ServerStartResultSuccess success;
}
struct ServerStartResultSuccessButWaiting {}
struct ServerStartResultSuccess
{
	1: string room_id;
	2: i32 port;
}

union Result
{
	1: ResultSuccess success;
	2: ResultFailure failure;
}
struct ResultSuccess {}
struct ResultFailure
{
	1: string result;
}
