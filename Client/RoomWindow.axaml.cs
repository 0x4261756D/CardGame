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
using CardGameUtils.Packets.Server;

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
				ServerPacket packet = await Task.Run(() => Functions.ReadSizedServerServerPacketFromStream(client.GetStream())).ConfigureAwait(false);
				await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(packet));
			}
		}
	}

	private void HandlePacket(ServerPacket packet)
	{
		switch(packet.ContentType)
		{
			case ServerContent.opponent_changed:
			{
				string? name = packet.ContentAsopponent_changed().Name;
				OpponentNameBlock.Text = name;
			}
			break;
			case ServerContent.start:
			{
				ServerStartPacket response = packet.ContentAsstart();
				if(response.ResultType == ServerStartResult.ServerStartResultSuccess)
				{
					StartGame(response.ResultAsServerStartResultSuccess().Port, response.ResultAsServerStartResultSuccess().RoomId);
				}
				else
				{
					_ = new ErrorPopup(response.ResultType == ServerStartResult.ServerStartResultFailure ? response.ResultAsServerStartResultFailure().Reason : "Starting failed for an unknown reason");
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
				stream.Write(ServerWindow.ClientPacketTToByteArray(new()
				{
					Content = new()
					{
						Type = ClientContent.leave,
						Value = new ClientLeavePacketT()
					}
				}));
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
		CardGameUtils.Shared.DeckInfo deckInfo;
		try
		{
			using TcpClient deckClient = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
			using NetworkStream stream = deckClient.GetStream();
			await stream.WriteAsync(DeckEditWindow.ClientPacketTToByteArray(new()
			{
				Content = new()
				{
					Type = CardGameUtils.Packets.Deck.ClientContent.list,
					Value = new CardGameUtils.Packets.Deck.ClientListPacketT
					{
						Name = deckname
					}
				}
			})).ConfigureAwait(false);
			CardGameUtils.Packets.Deck.ServerPacket packet = Functions.ReadSizedDeckServerPacketFromStream(stream);
			if(packet.ContentType != CardGameUtils.Packets.Deck.ServerContent.list)
			{
				throw new Exception($"Expected packet of type `list` but got {packet.ContentType}");
			}
			deckInfo = packet.ContentAslist().Deck!.Value;
		}
		catch(Exception e)
		{
			await new ErrorPopup(e.Message).ShowDialog(this).ConfigureAwait(false);
			return;
		}
		string[]? decklist = Functions.DeckInfoTToString(deckInfo.UnPack())?.Split('\n');
		if(decklist == null)
		{
			await new ErrorPopup("Deck list could not be loaded properly").ShowDialog(this).ConfigureAwait(false);
			return;
		}
		foreach(string card in decklist)
		{
			Functions.Log($"--{card}--");
		}
		try
		{
			client.GetStream().Write(ServerWindow.ClientPacketTToByteArray(new()
			{
				Content = new()
				{
					Type = ClientContent.start,
					Value = new ClientStartPacketT
					{
						Decklist = [.. decklist],
						Noshuffle = NoShuffleBox.IsChecked ?? false
					}
				}
			}));
			ServerPacket packet = Functions.ReadSizedServerServerPacketFromStream(client.GetStream());
			Functions.Log("Got start response");
			if(packet.ContentType != ServerContent.start)
			{
				throw new Exception($"Expected packet of type `start` but got {packet.ContentType}");
			}
			ServerStartPacket response = packet.ContentAsstart();
			if(response.ResultType == ServerStartResult.ServerStartResultFailure)
			{
				await new ErrorPopup(response.ResultAsServerStartResultFailure().Reason).ShowDialog(this).ConfigureAwait(false);
				return;
			}
			else
			{
				((Button)sender!).IsEnabled = false;
				if(response.ResultType == ServerStartResult.ServerStartResultSuccess)
				{
					StartGame(response.ResultAsServerStartResultSuccess().Port, response.ResultAsServerStartResultSuccess().RoomId);
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
		try
		{
			using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
			using NetworkStream stream = client.GetStream();
			stream.Write(DeckEditWindow.ClientPacketTToByteArray(new()
			{
				Content = new()
				{
					Type = CardGameUtils.Packets.Deck.ClientContent.names,
					Value = new CardGameUtils.Packets.Deck.ClientNamesPacketT(),
				}
			}));
			CardGameUtils.Packets.Deck.ServerPacket packet = Functions.ReadSizedDeckServerPacketFromStream(stream);
			if(packet.ContentType != CardGameUtils.Packets.Deck.ServerContent.names)
			{
				throw new Exception($"Expected packet of type `names` but got {packet.ContentType}");
			}
			Decknames.Clear();
			Decknames.AddRange(packet.ContentAsnames().UnPack().Names);
		}
		catch(Exception e)
		{
			new ErrorPopup(e.Message).Show();
		}
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
