using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Deck;
using Thrift.Protocol;
using Thrift.Transport.Server;
using Thrift.Transport.Client;
using Thrift.Transport;
using System.Threading.Tasks;

namespace CardGameCore;

partial class ClientCore : Core
{
	private readonly List<CardInfo> cards = [];
	private readonly List<DeckInfo> decks = [];
	private readonly CoreConfig.DeckConfig config;

	public ClientCore(CoreConfig.DeckConfig config, int port) : base(port)
	{
		Log($"Port: {port}");
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
			TryFetchAdditionalCards().Wait();
		}

		foreach(string deckfile in deckfiles)
		{
			List<string> decklist = [.. File.ReadAllLines(deckfile)];
			if(decklist.Count == 0)
			{
				continue;
			}
			DeckInfo deck = new()
			{
				Player_class = Enum.Parse<PlayerClass>(decklist[0]),
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
				deck.Cards = DecklistToCards(decklist);
			}
			else
			{
				deck.Cards = [];
			}
			decks.Add(deck);
		}
	}

	public override async Task Init(PipeStream? pipeStream)
	{
		await HandleNetworking();
		listener.Stop();
	}

	//TODO: This could be more elegant
	public List<CardInfo> DecklistToCards(List<string> decklist)
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
	public async Task TryFetchAdditionalCards()
	{
		try
		{
			TProtocol protocol = new TCompactProtocol(new TSocketTransport(host: config.additional_cards_url.address, port: config.additional_cards_url.port, new()));
			await new CardGameUtils.Packets.Server.ClientPacket.additional_cards(new()).WriteAsync(protocol, default);
			CardGameUtils.Packets.Server.ServerPacket packet = await CardGameUtils.Packets.Server.ServerPacket.ReadAsync(protocol, default);
			CardGameUtils.Packets.Server.ServerAdditionalCards? data = packet.As_additional_cards;
			if(data is null)
			{
				return;
			}
			if(data.Timestamp < Program.versionTimestamp)
			{
				Log($"Did not apply additional cards as they were older (client: {Program.versionTimestamp}, server: {data.Timestamp})");
				return;
			}
			if(data.Cards is null)
			{
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
	public override async Task HandleNetworking()
	{
		listener.Start();
		while(true)
		{
			Log("Waiting for a connection");
			using TcpClient client = await listener.AcceptTcpClientAsync();
			using NetworkStream stream = client.GetStream();
			Log("Got client");
			TTransport transport = new TSocketTransport(client, new());
			Log("Received a request");
			if(await HandlePacket(transport))
			{
				Log("Received a package that says the server should close");
				break;
			}
			Log("Sent a response");
		}
		listener.Stop();
	}

	public async Task<bool> HandlePacket(TTransport transport)
	{
		TProtocol protocol = new TCompactProtocol(transport);
		ClientPacket packet = await ClientPacket.ReadAsync(protocol, default);
		switch(packet)
		{
			case ClientPacket.names:
			{
				await new ServerPacket.names(new()
				{
					Names = decks.ConvertAll(x => x.Name!)
				}).WriteAsync(new TCompactProtocol(transport), default);
			}
			break;
			case ClientPacket.list:
			{
				Log($"{packet} {packet.As_list?.Name}");
				await new ServerPacket.list(new()
				{
					Deck = FindDeckByName(packet.As_list!.Name!)
				}).WriteAsync(new TCompactProtocol(transport), default);
			}
			break;
			case ClientPacket.search:
			{
				ClientSearch request = packet.As_search!;
				await new ServerPacket.search(new()
				{
					Cards = FilterCards(cards, request.Filter, request.Player_class, request.Include_generic_cards)
				}).WriteAsync(new TCompactProtocol(transport), default);
			}
			break;
			case ClientPacket.update_list:
			{
				DeckInfo? deck = packet.As_update_list?.Deck;
				if(deck is null)
				{
					break;
				}
				if(string.IsNullOrWhiteSpace(deck.Name))
				{
					break;
				}
				deck.Name = DeckNameRegex().Replace(deck.Name, "");
				int index = decks.FindIndex(x => x.Name == deck.Name);
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
			break;
			case ClientPacket.delete_list:
			{
				string? maybeName = packet.As_delete_list?.Name;
				if(string.IsNullOrWhiteSpace(maybeName))
				{
					break;
				}
				string name = DeckNameRegex().Replace(maybeName, "");
				int index = decks.FindIndex(x => x.Name == name);
				if(index != -1)
				{
					decks.RemoveAt(index);
					File.Delete(Path.Combine(config.deck_location, name + ".dek"));
				}
			}
			break;
			default:
			{
				Log($"Unknown server packet: {packet}", LogSeverity.Error);
				return true;
			}
		}
		return false;
	}

	private void SaveDeck(DeckInfo deck)
	{

		string? deckString = DeckInfoToString(deck);
		if(deckString == null)
		{
			return;
		}
		File.WriteAllText(Path.Combine(config.deck_location, deck.Name + ".dek"), deckString);
	}

	private static List<CardInfo> FilterCards(List<CardInfo> cards, string? filter, PlayerClass playerClass, bool includeGenericCards)
	{
		return cards.FindAll(card =>
			(playerClass == PlayerClass.All || (includeGenericCards && card.Card_class == PlayerClass.All) || card.Card_class == playerClass)
			&& card.ToString().Contains(filter ?? "", StringComparison.CurrentCultureIgnoreCase));
	}

	private DeckInfo FindDeckByName(string name)
	{
		name = DeckNameRegex().Replace(name, "");
		Log(name);
		return decks[decks.FindIndex(x => x.Name == name)];
	}

	[GeneratedRegex(@"[\./\\]")]
	private static partial Regex DeckNameRegex();
}
