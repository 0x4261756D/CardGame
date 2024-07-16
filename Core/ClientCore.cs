using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using CardGameUtils;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using CardGameUtils.Shared;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Deck;
using Google.FlatBuffers;
using System.Text;

namespace CardGameCore;

partial class ClientCore : Core
{
	private readonly List<CardInfoT> cards = [];
	private readonly List<DeckInfoT> decks = [];
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
			DeckInfoT deck = new()
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
				deck.Cards = DecklistToCards(decklist);
			}
			else
			{
				deck.Cards = [];
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
	public List<CardInfoT> DecklistToCards(List<string> decklist)
	{
		List<CardInfoT> c = [];
		foreach(string line in decklist)
		{
			int index = cards.FindIndex(x => x.Name == line);
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
			FlatBufferBuilder builder = new(1);
			builder.FinishSizePrefixed(CardGameUtils.Packets.Server.ClientPacket.Pack(builder, new CardGameUtils.Packets.Server.ClientPacketT
			{
				Content = new()
				{
					Type = CardGameUtils.Packets.Server.ClientContent.additional_cards,
					Value = new CardGameUtils.Packets.Server.ClientAdditionalCardsPacketT()
				}
			}).Value);
			stream.Write(builder.DataBuffer.ToSizedArray());
			CardGameUtils.Packets.Server.ServerPacket data = ReadSizedServerServerPacketFromStream(stream);
			if(data.ContentType != CardGameUtils.Packets.Server.ServerContent.additional_cards)
			{
				throw new Exception($"Expected packet of type additional_cards but got {data.ContentType}");
			}
			if(data.ContentAsadditional_cards().Timestamp < Program.versionTime)
			{
				Log($"Did not apply additional cards as they were older (client: {Program.versionTime}, server: {data.ContentAsadditional_cards().Timestamp})");
				return;
			}
			for(int i = 0; i < data.ContentAsadditional_cards().CardsLength; i++)
			{
				CardInfoT card = data.ContentAsadditional_cards().Cards(i)!.Value.UnPack();
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
			ClientPacket packet = Functions.ReadSizedDeckClientPacketFromStream(stream);
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

	public static byte[] ServerPacketTToByteArray(ServerPacketT packet)
	{
		FlatBufferBuilder builder = new(1);
		builder.FinishSizePrefixed(ServerPacket.Pack(builder, packet).Value);
		return builder.DataBuffer.ToSizedArray();
	}

	public bool HandlePacket(ClientPacket packet, NetworkStream stream)
	{
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		// possible improvements: Huffman or Burrows-Wheeler+RLE
		switch(packet.ContentType)
		{
			case ClientContent.names:
			{
				stream.Write(ServerPacketTToByteArray(new()
				{
					Content = new()
					{
						Type = ServerContent.names,
						Value = new ServerNamesPacketT
						{
							Names = decks.ConvertAll(x => x.Name),
						},
					}
				}));
			}
			break;
			case ClientContent.list:
			{
				stream.Write(ServerPacketTToByteArray(new()
				{
					Content = new()
					{
						Type = ServerContent.list,
						Value = new ServerListPacketT
						{
							Deck = FindDeckByName(packet.ContentAslist().Name)
						}
					}
				}));
			}
			break;
			case ClientContent.search:
			{
				ClientSearchPacket request = packet.ContentAssearch();
				stream.Write(ServerPacketTToByteArray(new()
				{
					Content = new()
					{
						Type = ServerContent.search,
						Value = new ServerSearchPacketT
						{
							Cards = FilterCards(cards, request.Filter, request.PlayerClass, request.IncludeGenericCards),
						},
					},
				}));
			}
			break;
			case ClientContent.update:
			{
				DeckInfo? deck = packet.ContentAsupdate().Deck;
				if(deck is null || !deck.HasValue)
				{
					break;
				}
				DeckInfoT deckT = deck.Value.UnPack();
				deckT.Name = DeckNameRegex().Replace(deckT.Name, "");
				int index = decks.FindIndex(x => x.Name == deckT.Name);
				if(index == -1)
				{
					decks.Add(deckT);
				}
				else
				{
					decks[index] = deckT;
				}
				SaveDeck(deckT);
			}
			break;
			case ClientContent.delete:
			{
				string sanitizedName = DeckNameRegex().Replace(packet.ContentAsdelete().Name, "");
				int index = decks.FindIndex(x => x.Name == sanitizedName);
				if(index != -1)
				{
					decks.RemoveAt(index);
					File.Delete(Path.Combine(config.deck_location, sanitizedName + ".dek"));
				}
			}
			break;
			default:
				throw new Exception($"ERROR: Unable to process this packet: ({packet.ContentType})");
		}
		return false;
	}

	private void SaveDeck(DeckInfoT deck)
	{
		string? deckString = Functions.DeckInfoTToString(deck);
		if(deckString == null)
		{
			return;
		}
		File.WriteAllText(Path.Combine(config.deck_location, deck.Name + ".dek"), deckString);
	}

	private static List<CardInfoT> FilterCards(List<CardInfoT> cards, string? filter, PlayerClass playerClass, bool includeGenericCards)
	{
		if(playerClass == PlayerClass.UNKNOWN)
		{
			playerClass = PlayerClass.All;
		}
		Functions.Log($"playerClass: {playerClass}, includeGenericCards: {includeGenericCards}, filter: {filter}");
		return cards.FindAll(card =>
			(playerClass == PlayerClass.All || (includeGenericCards && card.CardClass == PlayerClass.All) || card.CardClass == playerClass)
			&& (filter is null || Functions.CardInfoTToString(card).Contains(filter, StringComparison.CurrentCultureIgnoreCase)));
	}

	private DeckInfoT FindDeckByName(string name)
	{
		name = DeckNameRegex().Replace(name, "");
		return decks[decks.FindIndex(x => x.Name == name)];
	}

	[GeneratedRegex(@"[\./\\]")]
	private static partial Regex DeckNameRegex();

}
