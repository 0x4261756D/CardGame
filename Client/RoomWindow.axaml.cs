using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CardGameUtils;
using Google.Protobuf;
// using static CardGameUtils.Structs.NetworkingStructs;

namespace CardGameClient;

public partial class RoomWindow : Window
{
	private readonly Task networkTask;
	private readonly TcpClient client;
	private readonly string address;
	public bool closed;
	public RoomWindow(string address, TcpClient client, string? opponentName = null)
	{
		this.client = client;
		this.address = address;
		networkTask = new Task(HandleNetwork, TaskCreationOptions.LongRunning);
		networkTask.Start();
		DataContext = new RoomWindowViewModel();
		Closed += (sender, args) => CloseRoom();
		InitializeComponent();
		OpponentNameBlock.Text = opponentName;
		if(DeckSelectBox.ItemCount <= 0)
		{
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
				CardGameUtils.ServerServerToClient.Packet? packet = await Task.Run(() => TryReceiveRawPacket(client.GetStream(), 100)).ConfigureAwait(false);
				if(packet != null)
				{
					await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(packet));
				}
			}
		}
	}

	private static async Task<CardGameUtils.ServerServerToClient.Packet?>? TryReceiveRawPacket(NetworkStream stream, int timeoutMilliseconds)
	{
		if(!stream.CanRead)
		{
			return null;
		}
		Stopwatch watch = Stopwatch.StartNew();
		while(!stream.DataAvailable)
		{
			await Task.Delay(10).ConfigureAwait(false);
			if(!stream.CanRead || (timeoutMilliseconds != -1 && timeoutMilliseconds < watch.ElapsedMilliseconds))
			{
				return null;
			}
		}
		return CardGameUtils.ServerServerToClient.Packet.Parser.ParseDelimitedFrom(stream);
	}

	private void HandlePacket(CardGameUtils.ServerServerToClient.Packet packet)
	{
		switch(packet.KindCase)
		{
			case CardGameUtils.ServerServerToClient.Packet.KindOneofCase.OpponentChanged:
			{
				CardGameUtils.ServerServerToClient.OpponentChanged response = packet.OpponentChanged;
				string? name = response.HasName ? response.Name : null;
				OpponentNameBlock.Text = name;
			}
			break;
			case CardGameUtils.ServerServerToClient.Packet.KindOneofCase.Start:
			{
				if(packet.Start.ResultCase != CardGameUtils.ServerServerToClient.Start.ResultOneofCase.Success)
				{
					_ = new ErrorPopup(packet.Start.Failure?.Reason ?? "Duel creation failed for unknown reason");
				}
				else
				{
					StartGame(packet.Start.Success.Port, packet.Start.Success.Id);
				}
			}
			break;
			default:
			{
				throw new Exception($"Unexpected packet of type {packet.GetType()}");
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
			networkTask.Dispose();
			NetworkStream stream = client.GetStream();
			if(stream.Socket.Connected)
			{
				new CardGameUtils.ServerClientToServer.Packet { Leave = new() }.WriteDelimitedTo(stream);
			}
			client.Close();
			closed = true;
		}
	}
	private async void TryStartClick(object? sender, RoutedEventArgs args)
	{
		if(DeckSelectBox.SelectedItem is not string deckname || string.IsNullOrEmpty(deckname))
		{
			await new ErrorPopup("No deck selected").ShowDialog(this).ConfigureAwait(false);
			return;
		}
		if(OpponentNameBlock.Text is null or "")
		{
			await new ErrorPopup("You have no opponent").ShowDialog(this).ConfigureAwait(false);
			return;
		}
		using TcpClient newClient = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = newClient.GetStream();
		new CardGameUtils.DeckClientToServer.Packet { GetDecklist = new() { Name = deckname } }.WriteDelimitedTo(stream);
		CardGameUtils.DeckServerToClient.GetDecklist? listResponse = CardGameUtils.DeckServerToClient.GetDecklist.Parser.ParseDelimitedFrom(stream);
		string[]? decklist = Functions.DeckToString(listResponse.Deck)?.Split('\n');
		if(decklist == null)
		{
			await new ErrorPopup("Deck list could not be loaded properly").ShowDialog(this).ConfigureAwait(false);
			return;
		}
		try
		{
			CardGameUtils.ServerClientToServer.Start payload = new()
			{
				Noshuffle = NoShuffleBox.IsChecked ?? false
			};
			payload.Decklist.AddRange(decklist);
			new CardGameUtils.ServerClientToServer.Packet
			{
				Start = payload
			}.WriteDelimitedTo(client.GetStream());
			CardGameUtils.ServerServerToClient.Packet response = CardGameUtils.ServerServerToClient.Packet.Parser.ParseDelimitedFrom(client.GetStream());
			if(response.KindCase != CardGameUtils.ServerServerToClient.Packet.KindOneofCase.Start)
			{
				new ErrorPopup("Did not get a start response").Show();
				return;
			}
			if(response.Start.ResultCase == CardGameUtils.ServerServerToClient.Start.ResultOneofCase.Failure)
			{
				new ErrorPopup(response.Start.Failure.Reason).Show();
				return;
			}
			else
			{
				((Button)sender!).IsEnabled = false;
				if(response.Start.ResultCase == CardGameUtils.ServerServerToClient.Start.ResultOneofCase.Success)
				{
					StartGame(response.Start.Success.Port, response.Start.Success.Id);
				}
			}
		}
		catch(Exception ex)
		{
			new ErrorPopup(ex.Message).Show();
		}
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
}
public class RoomWindowViewModel : INotifyPropertyChanged
{
	public RoomWindowViewModel()
	{
		LoadDecks();
	}

	public void LoadDecks()
	{
		using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		new CardGameUtils.DeckClientToServer.Packet { Names = new() }.WriteDelimitedTo(stream);
		CardGameUtils.DeckServerToClient.Names packet = CardGameUtils.DeckServerToClient.Names.Parser.ParseDelimitedFrom(stream);
		Decknames.Clear();
		Decknames.AddRange(packet.Names_);
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private List<string> decknames = [];
	public List<string> Decknames
	{
		get => decknames;
		set
		{
			if(value != decknames)
			{
				decknames = value;
				NotifyPropertyChanged();
			}
		}
	}
}
