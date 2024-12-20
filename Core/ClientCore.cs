using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using CardGameUtils;
using static CardGameUtils.Functions;
using CardGameUtils.Base;
using CardGameUtils.GameEnumsAndStructs;
using System.Threading.Tasks;
using CardGameUtils.Structs.Deck;

namespace CardGameCore;

partial class ClientCore : Core
{
	private readonly List<CardStruct> cards = [];
	private readonly List<CardGameUtils.Base.Deck> decks = [];
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
			PlayerClass playerClass = Enum.Parse<PlayerClass>(decklist[0]);
			decklist.RemoveAt(0);
			List<CardStruct> deckCards = [];
			CardStruct? ability = null;
			CardStruct? quest = null;
			if(decklist.Count > 0)
			{
				if(decklist[0].StartsWith('#'))
				{
					ability = cards[cards.FindIndex(x => x.name == decklist[0][1..])];
					decklist.RemoveAt(0);
				}
				if(decklist[0].StartsWith('|'))
				{
					quest = cards[cards.FindIndex(x => x.name == decklist[0][1..])];
					decklist.RemoveAt(0);
				}
				deckCards = DecklistToCards(decklist);
			}
			decks.Add(new(player_class: playerClass, name: Path.GetFileNameWithoutExtension(deckfile), ability: ability, quest: quest, cards: deckCards));
		}
	}

	public override void Init(PipeStream? pipeStream)
	{
		HandleNetworking();
		listener.Stop();
	}

	//TODO: This could be more elegant
	public List<CardStruct> DecklistToCards(List<string> decklist)
	{
		List<CardStruct> c = [];
		foreach(string line in decklist)
		{
			if(string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			int index = cards.FindIndex(x => x.name == line);
			if(index >= 0)
			{
				c.Add(cards[index]);
			}
		}
		return c;
	}
	public static CardGameUtils.Structs.Server.SToC_Content? TryReceiveServerPacket(NetworkStream stream, int timeoutInMs)
	{
		try
		{
			Task task = Task.Run(() => { while(!stream.DataAvailable) { } return; });
			int i = Task.WaitAny(task, Task.Delay(timeoutInMs));
			return i == 0 ? CardGameUtils.Structs.Server.SToC_Packet.Deserialize(stream).content : null;
		}
		catch(Exception e)
		{
			Log(e.Message, severity: LogSeverity.Warning);
			return null;
		}
	}
	public void TryFetchAdditionalCards()
	{
		try
		{
			using TcpClient client = new(config.additional_cards_url.address, config.additional_cards_url.port);
			using NetworkStream stream = client.GetStream();
			stream.Write(new CardGameUtils.Structs.Server.CToS_Packet(new CardGameUtils.Structs.Server.CToS_Content.additional_cards()).Serialize());
			CardGameUtils.Structs.Server.SToC_Content? data = TryReceiveServerPacket(stream, 1000);
			if(data is null)
			{
				return;
			}
			if(data is CardGameUtils.Structs.Server.SToC_Response_AdditionalCards additionalCards)
			{
				if(additionalCards.timestamp < Program.versionTime)
				{
					Log($"Did not apply additional cards as they were older (client: {Program.versionTime}, server: {additionalCards.timestamp})");
					return;
				}
				foreach(CardStruct card in additionalCards.cards)
				{
					_ = cards.Remove(card);
					cards.Add(card);
				}
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
			CToS_Content packet = CToS_Packet.Deserialize(stream).content;
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

	public bool HandlePacket(CToS_Content packet, NetworkStream stream)
	{
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		// possible improvements: Huffman or Burrows-Wheeler+RLE
		byte[] payload;
		switch(packet)
		{
			case CToS_Content.decklists:
			{
				payload = new SToC_Packet(new SToC_Content.decklists(new(decks.ConvertAll(x => x.name)))).Serialize();
			}
			break;
			case CToS_Content.decklist request:
			{
				payload = new SToC_Packet(new SToC_Content.decklist(new(FindDeckByName(request.value.name)))).Serialize();
			}
			break;
			case CToS_Content.search request:
			{
				payload = new SToC_Packet(new SToC_Content.search(new(FilterCards(cards, request.value.filter, request.value.player_class, request.value.include_generic_cards)))).Serialize();
			}
			break;
			case CToS_Content.decklist_update request:
			{
				CardGameUtils.Base.Deck deck = request.value.deck;
				string sanitizedName = DeckNameRegex().Replace(deck.name, "");
				int index = decks.FindIndex(x => x.name == sanitizedName);
				if(index == -1)
				{
					decks.Add(deck);
				}
				else
				{
					decks[index] = deck;
				}
				SaveDeck(sanitizedName, deck);
				return false;
			}
			case CToS_Content.decklist_delete request:
			{
				int index = decks.FindIndex(x => x.name == request.value.name);
				if(index == -1)
				{
					return false;
				}
				string name = DeckNameRegex().Replace(decks[index].name, "");
				decks.RemoveAt(index);
				File.Delete(Path.Combine(config.deck_location, name + ".dek"));
				return false;
			}
			default:
				throw new Exception($"ERROR: Unable to process this packet: ({packet.GetType()})");
		}
		stream.Write(payload);
		return false;
	}

	private void SaveDeck(string sanitizedName, CardGameUtils.Base.Deck deck)
	{
		string? deckString = GetDeckString(deck);
		if(deckString == null)
		{
			return;
		}
		File.WriteAllText(Path.Combine(config.deck_location, sanitizedName + ".dek"), deckString);
	}

	private static List<CardStruct> FilterCards(List<CardStruct> cards, string filter, PlayerClass playerClass, bool includeGenericCards)
	{
		return cards.FindAll(card =>
			(playerClass == PlayerClass.All || (includeGenericCards && card.card_class == PlayerClass.All) || card.card_class == playerClass)
			&& FormatCardStruct(card).Contains(filter, StringComparison.CurrentCultureIgnoreCase));
	}

	private CardGameUtils.Base.Deck? FindDeckByName(string name)
	{
		name = DeckNameRegex().Replace(name, "");
		int index = decks.FindIndex(x => x.name == name);
		return index == -1 ? null : decks[index];
	}

	[GeneratedRegex(@"[\./\\]")]
	private static partial Regex DeckNameRegex();
}
