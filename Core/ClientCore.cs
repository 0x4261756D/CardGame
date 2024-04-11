using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CardGameUtils;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using static CardGameUtils.Structs.NetworkingStructs;

namespace CardGameCore;

partial class ClientCore : Core
{
	private readonly List<CardStruct> cards = [];
	private readonly List<DeckPackets.Deck> decks = [];
	private readonly CoreConfig.DeckConfig config;
	public ClientCore(CoreConfig.DeckConfig config, int port) : base(port)
	{
		this.config = config;
		if(!Directory.Exists(config.deck_location))
		{
			Log($"Deck folder not found, creating it at {config.deck_location}", LogSeverity.Warning);
			_ = Directory.CreateDirectory(config.deck_location);
		}
		string[] deckfiles = Directory.GetFiles(config.deck_location);
		foreach(Type card in Array.FindAll(Assembly.GetExecutingAssembly().GetTypes(), Program.IsCardSubclass))
		{
			Card c = (Card)Activator.CreateInstance(card)!;
			cards.Add(c.ToStruct(client: true));
		}

		if(config.should_fetch_additional_cards)
		{
			TryFetchAdditionalCards();
		}

		foreach(string deckfile in deckfiles)
		{
			List<string> decklist = [.. File.ReadAllLines(deckfile)];
			if(decklist.Count == 0)
			{
				continue;
			}
			DeckPackets.Deck deck = new()
			{
				player_class = Enum.Parse<GameConstants.PlayerClass>(decklist[0]),
				name = Path.GetFileNameWithoutExtension(deckfile)
			};
			decklist.RemoveAt(0);
			if(decklist.Count > 0)
			{
				if(decklist[0].StartsWith('#'))
				{
					deck.ability = cards[cards.FindIndex(x => x.name == decklist[0][1..])];
					decklist.RemoveAt(0);
				}
				if(decklist[0].StartsWith('|'))
				{
					deck.quest = cards[cards.FindIndex(x => x.name == decklist[0][1..])];
					decklist.RemoveAt(0);
				}
				deck.cards = DecklistToCards(decklist);
			}
			else
			{
				deck.cards = [];
			}
			decks.Add(deck);
		}
	}

	public override void Init(PipeStream? pipeStream)
	{
		HandleNetworking();
		listener.Stop();
	}

	//TODO: This could be more elegant
	public CardStruct[] DecklistToCards(List<string> decklist)
	{
		List<CardStruct> c = [];
		foreach(string line in decklist)
		{
			int index = cards.FindIndex(x => x.name == line);
			if(index >= 0)
			{
				c.Add(cards[index]);
			}
		}
		return [.. c];
	}
	public void TryFetchAdditionalCards()
	{
		try
		{
			using TcpClient client = new(config.additional_cards_url.address, config.additional_cards_url.port);
			using NetworkStream stream = client.GetStream();
			stream.Write(GeneratePayload(new ServerPackets.AdditionalCardsRequest()));
			ServerPackets.AdditionalCardsResponse? data = TryReceivePacket<ServerPackets.AdditionalCardsResponse>(stream, 1000);
			if(data == null)
			{
				return;
			}
			if(data.time < Program.versionTime)
			{
				Log($"Did not apply additional cards as they were older (client: {Program.versionTime}, server: {data.time})");
				return;
			}
			foreach(CardStruct card in data.cards)
			{
				_ = cards.Remove(card);
				cards.Add(card);
			}
		}
		catch(Exception e)
		{
			Log($"Could not fetch additional cards {e.Message}", severity: LogSeverity.Warning);
		}
	}
	public override void HandleNetworking()
	{
		listener.Start();
		while(true)
		{
			Log("Waiting for a connection");
			using TcpClient client = listener.AcceptTcpClient();
			using NetworkStream stream = client.GetStream();
			Packet packet = ReceiveRawPacket(stream);
			Log("Received a request");
			if(HandlePacket(packet, stream))
			{
				Log("Received a package that says the server should close");
				break;
			}
			Log("Sent a response");
		}
		listener.Stop();
	}

	public bool HandlePacket(Packet packet, NetworkStream stream)
	{
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		// possible improvements: Huffman or Burrows-Wheeler+RLE
		byte[] payload;
		switch(packet)
		{
			case DeckPackets.NamesRequest:
			{
				payload = GeneratePayload(new DeckPackets.NamesResponse
				(
					names: [.. decks.ConvertAll(x => x.name)]
				));
			}
			break;
			case DeckPackets.ListRequest request:
			{
				payload = GeneratePayload(new DeckPackets.ListResponse
				(
					deck: FindDeckByName(request.name!)
				));
			}
			break;
			case DeckPackets.SearchRequest request:
			{
				payload = GeneratePayload(new DeckPackets.SearchResponse
				(
					cards: FilterCards(cards, request.filter!, request.playerClass, request.includeGenericCards)
				));
			}
			break;
			case DeckPackets.ListUpdateRequest request:
			{
				DeckPackets.Deck deck = request.deck;
				deck.name = DeckNameRegex().Replace(deck.name, "");
				int index = decks.FindIndex(x => x.name == deck.name);
				if(deck.cards != null)
				{
					if(index == -1)
					{
						decks.Add(deck);
					}
					else
					{
						decks[index] = deck;
					}
					SaveDeck(deck);
				}
				else
				{
					if(index != -1)
					{
						decks.RemoveAt(index);
						File.Delete(Path.Combine(config.deck_location, deck.name + ".dek"));
					}
				}
				payload = GeneratePayload(new DeckPackets.ListUpdateResponse(shouldUpdate: index == -1));
			}
			break;
			default:
				throw new Exception($"ERROR: Unable to process this packet: ({packet.GetType()}) | {JsonSerializer.Serialize(packet, options: GenericConstants.packetSerialization)}");
		}
		stream.Write(payload);
		return false;
	}

	private void SaveDeck(DeckPackets.Deck deck)
	{
		string? deckString = deck.ToString();
		if(deckString == null)
		{
			return;
		}
		File.WriteAllText(Path.Combine(config.deck_location, deck.name + ".dek"), deckString);
	}

	private static CardStruct[] FilterCards(List<CardStruct> cards, string filter, GameConstants.PlayerClass playerClass, bool includeGenericCards)
	{
		return Array.FindAll(cards.ToArray(), card =>
			(playerClass == GameConstants.PlayerClass.All || (includeGenericCards && card.card_class == GameConstants.PlayerClass.All) || card.card_class == playerClass)
			&& card.ToString().Contains(filter, StringComparison.CurrentCultureIgnoreCase));
	}

	private DeckPackets.Deck FindDeckByName(string name)
	{
		name = DeckNameRegex().Replace(name, "");
		return decks[decks.FindIndex(x => x.name == name)];
	}

	[GeneratedRegex(@"[\./\\]")]
	private static partial Regex DeckNameRegex();
}
