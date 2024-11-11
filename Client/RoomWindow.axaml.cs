using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.Structs.Server;

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
	public static SToC_Content? TryReceivePacket(NetworkStream stream, int timeoutInMs)
	{
		Monitor.Enter(stream);
		try
		{
			Task task = Task.Run(() => { while(!stream.DataAvailable) { } return; });
			int i = Task.WaitAny(task, Task.Delay(timeoutInMs));
			Monitor.Exit(stream);
			return i == 0 ? SToC_Packet.Serialize(stream).content : null;
		}
		catch(Exception e)
		{
			Functions.Log(e.Message, severity: Functions.LogSeverity.Warning);
			Monitor.Exit(stream);
			return null;
		}
	}

	public static T? TrySendAndReceive<T>(CToS_Content content, string address, int port) where T : SToC_Content
	{
		try
		{
			using TcpClient c = new(address, port);
			using NetworkStream stream = c.GetStream();
			stream.Write(new CToS_Packet(content).Deserialize());
			return (T)SToC_Packet.Serialize(stream).content;
		}
		catch(Exception ex)
		{
			new ErrorPopup(ex.Message).Show();
			return default;
		}
	}

	private async void HandleNetwork()
	{
		while(!closed)
		{
			if(client.Connected)
			{
				SToC_Content? content = await Task.Run(() => TryReceivePacket(client.GetStream(), 100)).ConfigureAwait(false);
				if(content is not null)
				{
					await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(content));
				}
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
						Functions.Log("Unexpected success_but_waiting received");
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
			networkTask.Dispose();
			NetworkStream stream = client.GetStream();
			if(stream.Socket.Connected)
			{
				stream.Write(new CToS_Packet(new CToS_Content.leave()).Deserialize());
			}
			client.Close();
			closed = true;
		}
	}
	private async void TryStartClick(object? sender, RoutedEventArgs args)
	{
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
		if(deck is null)
		{
			return;
		}
		try
		{
			NetworkStream s = client.GetStream();
			Monitor.Enter(s);
			s.Write(new CToS_Packet(new CToS_Content.start(new
			(
				decklist: deck,
				no_shuffle: NoShuffleBox.IsChecked ?? false
			))).Deserialize());
			SToC_Response_Start content = ((SToC_Content.start)SToC_Packet.Serialize(s).content).value;
			Monitor.Exit(s);
			switch(content)
			{
				case SToC_Response_Start.success success:
				{
					StartGame(success.value.port, success.value.id);
				}
				break;
				case SToC_Response_Start.success_but_waiting:
				{
					((Button)sender).IsEnabled = false;
					((Button)sender).Content = "Waiting";
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
		List<string>? names = DeckEditWindow.TrySendAndReceive<CardGameUtils.Structs.Deck.SToC_Content.decklists>(new CardGameUtils.Structs.Deck.CToS_Content.decklists(), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port)?.value.names;
		if(names is null)
		{
			return;
		}
		Decknames.Clear();
		Decknames.AddRange(names);
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
