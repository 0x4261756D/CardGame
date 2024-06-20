using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using CardGameUtils.CardConstants;
using CardGameUtils.Structs;
using Google.Protobuf;
using static CardGameUtils.Functions;

namespace CardGameCore;

partial class ClientCore : Core
{
	private readonly List<CardInfo> cards = [];
	private readonly List<CardGameUtils.CardConstants.Deck> decks = [];
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
			CardGameUtils.CardConstants.Deck deck = new()
			{
				PlayerClass = Enum.Parse<PlayerClass>(decklist[0]),
				Name = Path.GetFileNameWithoutExtension(deckfile)
			};
			decklist.RemoveAt(0);
			if(decklist.Count > 0)
			{
				if(decklist[0].StartsWith('#'))
				{
					deck.Ability = cards[cards.FindIndex(x => x.Name == decklist[0][1..])];
					decklist.RemoveAt(0);
				}
				if(decklist[0].StartsWith('|'))
				{
					deck.Quest = cards[cards.FindIndex(x => x.Name == decklist[0][1..])];
					decklist.RemoveAt(0);
				}
				deck.Cards.AddRange(DecklistToCards(decklist));
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
	public IEnumerable<CardInfo> DecklistToCards(List<string> decklist)
	{
		List<CardInfo> c = [];
		foreach(string line in decklist)
		{
			int index = cards.FindIndex(x => x.Name == line);
			if(index >= 0)
			{
				c.Add(cards[index]);
			}
		}
		return c;
	}
	public void TryFetchAdditionalCards()
	{
		try
		{
			using TcpClient client = new(config.additional_cards_url.address, config.additional_cards_url.port);
			using NetworkStream stream = client.GetStream();
			new CardGameUtils.ServerClientToServer.Packet { AdditionalCards = new() }.WriteDelimitedTo(stream);
			if(!stream.CanRead)
			{
				return;
			}
			Stopwatch watch = Stopwatch.StartNew();
			while(!stream.DataAvailable)
			{
				Thread.Sleep(10);
				if(!stream.CanRead || watch.ElapsedMilliseconds > 1000)
				{
					return;
				}
			}
			CardGameUtils.ServerServerToClient.Packet packet = CardGameUtils.ServerServerToClient.Packet.Parser.ParseDelimitedFrom(stream);

			CardGameUtils.ServerServerToClient.AdditionalCards data = packet.AdditionalCards;
			if(data.Timestamp.ToDateTime() < Program.versionTime)
			{
				Log($"Did not apply additional cards as they were older (client: {Program.versionTime}, server: {data.Timestamp.ToDateTime()})");
				return;
			}
			foreach(CardInfo card in data.Cards)
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
			CardGameUtils.DeckClientToServer.Packet packet = CardGameUtils.DeckClientToServer.Packet.Parser.ParseDelimitedFrom(stream);
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

	public bool HandlePacket(CardGameUtils.DeckClientToServer.Packet packet, NetworkStream stream)
	{
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		// possible improvements: Huffman or Burrows-Wheeler+RLE
		switch(packet.KindCase)
		{
			case CardGameUtils.DeckClientToServer.Packet.KindOneofCase.Names:
			{
				CardGameUtils.DeckServerToClient.Names payload = new();
				payload.Names_.AddRange(decks.ConvertAll(x => x.Name));
				payload.WriteDelimitedTo(stream);
			}
			break;
			case CardGameUtils.DeckClientToServer.Packet.KindOneofCase.GetDecklist:
			{
				new CardGameUtils.DeckServerToClient.GetDecklist
				{
					Deck = FindDeckByName(packet.GetDecklist.Name)
				}.WriteDelimitedTo(stream);
			}
			break;
			case CardGameUtils.DeckClientToServer.Packet.KindOneofCase.Search:
			{
				CardGameUtils.DeckServerToClient.Search payload = new();
				payload.Cards.AddRange(FilterCards(cards, packet.Search.Filter, packet.Search.PlayerClass, packet.Search.IncludeGenericCards));
				payload.WriteDelimitedTo(stream);
			}
			break;
			case CardGameUtils.DeckClientToServer.Packet.KindOneofCase.UpdateDecklist:
			{
				CardGameUtils.CardConstants.Deck deck = packet.UpdateDecklist.Deck;
				deck.Name = DeckNameRegex().Replace(deck.Name, "");
				int index = decks.FindIndex(x => x.Name == deck.Name);
				if(deck.Cards is not null)
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
			}
			break;
			case CardGameUtils.DeckClientToServer.Packet.KindOneofCase.DeleteDecklist:
			{
				int index = decks.FindIndex(x => x.Name == packet.DeleteDecklist.Name);
				if(index != -1)
				{
					decks.RemoveAt(index);
					File.Delete(Path.Combine(config.deck_location, packet.DeleteDecklist.Name + ".dek"));
				}
			}
			break;
			default:
				throw new Exception($"ERROR: Unable to process this packet: ({packet.KindCase})");
		}
		return false;
	}

	private void SaveDeck(CardGameUtils.CardConstants.Deck deck)
	{
		string? deckString = DeckToString(deck);
		if(deckString == null)
		{
			return;
		}
		File.WriteAllText(Path.Combine(config.deck_location, deck.Name + ".dek"), deckString);
	}

	private static CardInfo[] FilterCards(List<CardInfo> cards, string filter, PlayerClass playerClass, bool includeGenericCards)
	{
		return Array.FindAll(cards.ToArray(), card =>
			(playerClass == PlayerClass.All || (includeGenericCards && card.CardClass == PlayerClass.All) || card.CardClass == playerClass)
			&& card.ToString().Contains(filter, StringComparison.CurrentCultureIgnoreCase));
	}

	private CardGameUtils.CardConstants.Deck FindDeckByName(string name)
	{
		name = DeckNameRegex().Replace(name, "");
		Log(name, LogSeverity.Warning);
		return decks[decks.FindIndex(x => x.Name == name)];
	}

	[GeneratedRegex(@"[\./\\]")]
	private static partial Regex DeckNameRegex();
}
