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
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;
using System.Threading;

namespace CardGameClient;

public partial class RoomWindow : Window
{
	private readonly Task networkTask;
	private readonly TcpClient client;
	private readonly string address;
	public bool closed;
	private CancellationTokenSource cancellationTokenSource;

	public RoomWindow(string address, TcpClient client, string? opponentName = null)
	{
		this.cancellationTokenSource = new();
		this.client = client;
		this.address = address;
		networkTask = HandleNetwork(cancellationTokenSource.Token);
		DataContext = new RoomWindowViewModel();
		Closed += async (sender, args) => await CloseRoom();
		InitializeComponent();
		OpponentNameBlock.Text = opponentName;
		if(DeckSelectBox.ItemCount <= 0)
		{
			CloseRoom().Wait();
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

	private async Task HandleNetwork(CancellationToken token)
	{
		while(!closed && !token.IsCancellationRequested)
		{
			Functions.Log($"Closed: {closed}, cancel: {token.IsCancellationRequested}");
			if(client.Connected)
			{
				if(client.Available > 0)
				{
					CardGameUtils.Packets.Server.ServerPacket packet = await CardGameUtils.Packets.Server.ServerPacket.ReadAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
					await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(packet));
				}
				else
				{
					await Task.Delay(100);
				}
			}
		}
		Functions.Log("Returned");
		return;
	}

	private void HandlePacket(CardGameUtils.Packets.Server.ServerPacket packet)
	{
		switch(packet)
		{
			case CardGameUtils.Packets.Server.ServerPacket.opponent_changed:
			{
				OpponentNameBlock.Text = packet.As_opponent_changed!.Name;
			}
			break;
			case CardGameUtils.Packets.Server.ServerPacket.start:
			{
				CardGameUtils.Packets.Server.ServerStartResult response = packet.As_start!.Result!;
				if(response is not CardGameUtils.Packets.Server.ServerStartResult.success)
				{
					_ = new ErrorPopup(response.As_failure?.Result ?? "Duel creation failed for unknown reason");
				}
				else
				{
					StartGame(response.As_success!.Port, response.As_success.Room_id!);
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
	public async void BackClick(object? sender, RoutedEventArgs? args)
	{
		await CloseRoom();
		new ServerWindow
		{
			WindowState = WindowState,
		}.Show();
		Functions.Log("here");
		Close();
	}
	public async Task CloseRoom()
	{
		Functions.Log("Closing");
		if(!closed)
		{
			await cancellationTokenSource.CancelAsync();
			cancellationTokenSource.Dispose();
			Functions.Log("nw task disposed");
			if(client.Connected)
			{
				await new CardGameUtils.Packets.Server.ClientPacket.leave(new()).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
				Functions.Log("left");
			}
			client.Close();
			closed = true;
			Functions.Log("Close done");
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
		CardGameUtils.Packets.Deck.ServerPacket? listResponse = null;
		try
		{
			TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, timeout: 100, config: new());
			await new CardGameUtils.Packets.Deck.ClientPacket.list(new() { Name = deckname }).WriteAsync(new TCompactProtocol(transport), default);
			listResponse = await CardGameUtils.Packets.Deck.ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		}
		catch(Exception e)
		{
			new ErrorPopup(e.Message).Show();
			return;
		}
		if(listResponse == null)
		{
			return;
		}
		string[]? decklist = Functions.DeckInfoToString(listResponse.As_list!.Deck!)?.Split('\n');
		if(decklist == null)
		{
			await new ErrorPopup("Deck list could not be loaded properly").ShowDialog(this).ConfigureAwait(false);
			return;
		}
		try
		{
			TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, timeout: 100, config: new());
			await new CardGameUtils.Packets.Server.ClientPacket.start(new()
			{
				Decklist = [.. decklist],
				Noshuffle = NoShuffleBox.IsChecked ?? false
			}).WriteAsync(new TCompactProtocol(transport), default);
			CardGameUtils.Packets.Server.ServerPacket packet = await CardGameUtils.Packets.Server.ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
			CardGameUtils.Packets.Server.ServerStartResult response = packet.As_start!.Result!;
			if(response is CardGameUtils.Packets.Server.ServerStartResult.failure)
			{
				new ErrorPopup(response.As_failure!.Result ?? "Failed to start for unknown reasons").Show();
				return;
			}
			else
			{
				((Button)sender!).IsEnabled = false;
				if(response is CardGameUtils.Packets.Server.ServerStartResult.success)
				{
					StartGame(response.As_success!.Port, response.As_success!.Room_id!);
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
		LoadDecks().Wait();
	}

	public async Task LoadDecks()
	{
		TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, timeout: 1000, config: new());
		await new CardGameUtils.Packets.Deck.ClientPacket.names(new()).WriteAsync(new TCompactProtocol(transport), default);
		CardGameUtils.Packets.Deck.ServerPacket packet = await CardGameUtils.Packets.Deck.ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		if(packet == null)
		{
			return;
		}
		Decknames.Clear();
		Decknames.AddRange(packet.As_names?.Names!);
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
