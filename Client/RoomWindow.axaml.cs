using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.Structs.Server;
using System.Diagnostics;

namespace CardGameClient;

internal partial class RoomWindow : Window
{
	private readonly Task networkTask;
	// This client is owned by this window, it is only passed in to handle potential errors in ServerWindow instead.
	private readonly TcpClient client;
	private readonly CancellationTokenSource cts = new();
	private readonly string address;
	public bool closed;
	public RoomWindow(string address, TcpClient client, string? opponentName = null)
	{
		this.client = client;
		this.address = address;
		this.closed = false;
		networkTask = new Task(HandleNetwork, TaskCreationOptions.LongRunning);
		networkTask.Start();
		Closed += (sender, args) => CloseRoom();
		InitializeComponent();
		LoadDecks();
		OpponentNameBlock.Text = opponentName;
		if(DeckSelectBox.ItemCount <= 0)
		{
			_ = new ErrorPopup("You have no decks to choose from");
			CloseRoom();
		}
		else
		{
			if(DeckSelectBox.SelectedItem == null && DeckSelectBox.ItemCount > 0)
			{
				if(Program.config.last_deck_name != null)
				{
					foreach(var item in DeckSelectBox.Items)
					{
						if((string?)item == Program.config.last_deck_name)
						{
							DeckSelectBox.SelectedItem = item;
						}
					}
				}
				else
				{
					DeckSelectBox.SelectedIndex = 0;
				}
			}
		}
	}

	private async void HandleNetwork()
	{
		while(!closed)
		{
			if(client.Connected)
			{
				SToC_Content content;
				try
				{
					content = (await SToC_Packet.DeserializeAsync(client.GetStream(), cts.Token)).content;
				}
				catch(OperationCanceledException)
				{
					break;
				}
				if(content is not null)
				{
					await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(content));
				}
				Thread.Sleep(100);
			}
		}
	}

	private void HandlePacket(SToC_Content content)
	{
		switch(content)
		{
			case SToC_Content.opponent_changed response:
			{
				OpponentNameBlock.Text = response.value.name;
			}
			break;
			case SToC_Content.start response:
			{
				switch(response.value)
				{
					case SToC_Response_Start.success_but_waiting:
					{
						TryStartButton.IsEnabled = false;
						TryStartButton.Content = "Waiting";
					}
					break;
					case SToC_Response_Start.success success:
					{
						StartGame(success.value.port, success.value.id);
					}
					break;
					case SToC_Response_Start.failure failure:
					{
						new ErrorPopup(failure.value).Show();
					}
					break;
					default:
						throw new NotImplementedException();
				}
			}
			break;
			default:
			{
				throw new Exception($"Unexpected packet of type {content.GetType()}");
			}
		}
	}

	public void DeckSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		Program.config.last_deck_name = args.AddedItems[0]?.ToString();
	}
	public void BackClick(object? sender, RoutedEventArgs? args)
	{
		CloseRoom();
		new ServerWindow
		{
			WindowState = WindowState,
		}.Show();
		Close();
	}
	public void CloseRoom()
	{
		if(!closed)
		{
			cts.Cancel();
			networkTask.Dispose();
			NetworkStream stream = client.GetStream();
			if(stream.Socket.Connected)
			{
				stream.Write(new CToS_Packet(new CToS_Content.leave()).Serialize());
			}
			client.Close();
			closed = true;
		}
	}
	private async void TryStartClick(object? sender, RoutedEventArgs args)
	{
		Stopwatch watch = Stopwatch.StartNew();
		if(sender is null)
		{
			return;
		}
		if(DeckSelectBox.SelectedItem is not string deckname || string.IsNullOrEmpty(deckname))
		{
			await new ErrorPopup("No deck selected").ShowDialog(this).ConfigureAwait(false);
			return;
		}
		Deck? deck = DeckEditWindow.TrySendAndReceive<CardGameUtils.Structs.Deck.SToC_Content.decklist>(new CardGameUtils.Structs.Deck.CToS_Content.decklist(new(name: deckname)),
			Program.config.deck_edit_url.address, Program.config.deck_edit_url.port)?.value.deck;
		Functions.Log($"Received deck after {watch.ElapsedMilliseconds} ms.");
		if(deck is null)
		{
			return;
		}
		try
		{
			NetworkStream s = client.GetStream();
			s.Write(new CToS_Packet(new CToS_Content.start(new
			(
				decklist: deck,
				no_shuffle: NoShuffleBox.IsChecked ?? false
			))).Serialize());
		}
		catch(Exception ex)
		{
			new ErrorPopup(ex.Message).Show();
		}
		Functions.Log($"Done after {watch.ElapsedMilliseconds} ms.");
	}

	public async void StartGame(int port, string id)
	{
		TcpClient duelClient = new();
		await duelClient.ConnectAsync(address, port).ConfigureAwait(false);
		byte[] idBytes = Encoding.UTF8.GetBytes(id);
		await duelClient.GetStream().WriteAsync(idBytes).ConfigureAwait(false);
		byte[] playerIndex = new byte[1];
		await duelClient.GetStream().ReadExactlyAsync(playerIndex, 0, 1).ConfigureAwait(false);
		await Dispatcher.UIThread.InvokeAsync(() =>
		{
			new DuelWindow(playerIndex[0], duelClient)
			{
				WindowState = WindowState,
			}.Show();
			Close();
		}, priority: DispatcherPriority.Background);
	}
	public void LoadDecks()
	{
		List<string>? names = DeckEditWindow.TrySendAndReceive<CardGameUtils.Structs.Deck.SToC_Content.decklists>(new CardGameUtils.Structs.Deck.CToS_Content.decklists(), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port)?.value.names;
		if(names is null)
		{
			return;
		}
		DeckSelectBox.Items.Clear();
		foreach(string name in names)
		{
			_ = DeckSelectBox.Items.Add(name);
		}
	}
}
