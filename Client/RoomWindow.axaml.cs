using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CardGameUtils;
using static CardGameUtils.Structs.NetworkingStructs;

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
				Packet? packet = await Task.Run(() => Functions.TryReceiveRawPacket(client.GetStream(), 100)).ConfigureAwait(false);
				if(packet != null)
				{
					await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(packet));
				}
			}
		}
	}

	private void HandlePacket(Packet packet)
	{
		switch(packet)
		{
			case ServerPackets.OpponentChangedResponse response:
			{
				string? name = response.name;
				OpponentNameBlock.Text = name;
			}
			break;
			case ServerPackets.StartResponse response:
			{
				if(response.success != ServerPackets.StartResponse.Result.Success)
				{
					_ = new ErrorPopup(response.reason ?? "Duel creation failed for unknown reason");
				}
				else
				{
					StartGame(response.port, response.id!);
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
				stream.Write(Functions.GeneratePayload(new ServerPackets.LeaveRequest()));
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
		DeckPackets.ListResponse? listResponse = UIUtils.TrySendAndReceive<DeckPackets.ListResponse>(new DeckPackets.ListRequest(name: deckname),
			Program.config.deck_edit_url.address, Program.config.deck_edit_url.port, this);
		if(listResponse == null)
		{
			return;
		}
		string[]? decklist = listResponse.deck.ToString()?.Split('\n');
		if(decklist == null)
		{
			await new ErrorPopup("Deck list could not be loaded properly").ShowDialog(this).ConfigureAwait(false);
			return;
		}
		try
		{
			client.GetStream().Write(Functions.GeneratePayload(new ServerPackets.StartRequest
			(
				decklist: decklist,
				noshuffle: NoShuffleBox.IsChecked ?? false
			)));
			ServerPackets.StartResponse response = Functions.ReceivePacket<ServerPackets.StartResponse>(client.GetStream());
			if(response.success == ServerPackets.StartResponse.Result.Failure)
			{
				new ErrorPopup(response.reason!).Show();
				return;
			}
			else
			{
				((Button)sender!).IsEnabled = false;
				if(response.success == ServerPackets.StartResponse.Result.Success)
				{
					StartGame(response.port, response.id!);
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
		DeckPackets.NamesResponse? packet = UIUtils.TrySendAndReceive<DeckPackets.NamesResponse>(new DeckPackets.NamesRequest(), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port, null);
		if(packet == null)
		{
			return;
		}
		Decknames.Clear();
		Decknames.AddRange(packet.names);
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
