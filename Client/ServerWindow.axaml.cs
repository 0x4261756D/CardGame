using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.CardConstants;
using Google.Protobuf;

namespace CardGameClient;

public partial class ServerWindow : Window
{
	public ServerWindow()
	{
		DataContext = new ServerWindowViewModel();
		InitializeComponent();
		UpdateRoomList();
	}
	public void BackClick(object sender, RoutedEventArgs args)
	{
		new MainWindow
		{
			WindowState = WindowState,
		}.Show();
		Close();
	}
	private void UpdateRoomList()
	{
		if(ServerAddressBox.Text == null)
		{
			new ErrorPopup("Connection to the server timed out").Show();
			return;
		}
		try
		{
			using TcpClient updateClient = new(ServerAddressBox.Text, GenericConstants.SERVER_PORT);
			using NetworkStream updateStream = updateClient.GetStream();
			new CardGameUtils.ServerClientToServer.Packet { Rooms = new() }.WriteDelimitedTo(updateStream);
			((ServerWindowViewModel)DataContext!).ServerRooms = [.. CardGameUtils.ServerServerToClient.Rooms.Parser.ParseDelimitedFrom(updateStream).Rooms_];
		}
		catch(Exception ex)
		{
			if(IsVisible)
			{
				new ErrorPopup(ex.Message).Show();
			}
			return;
		}
	}
	private void HostClick(object? sender, RoutedEventArgs args)
	{
		if(ServerAddressBox.Text == null)
		{
			return;
		}
		string playerName = ((ServerWindowViewModel)DataContext!).PlayerName;
		TcpClient client;
		try
		{
			client = new(ServerAddressBox.Text, GenericConstants.SERVER_PORT);
		}
		catch(Exception ex)
		{
			if(IsVisible)
			{
				new ErrorPopup(ex.Message).Show();
			}
			return;
		}
		new CardGameUtils.ServerClientToServer.Packet { Create = new() { Name = playerName } }.WriteDelimitedTo(client.GetStream());
		CardGameUtils.ServerServerToClient.Create response = CardGameUtils.ServerServerToClient.Create.Parser.ParseDelimitedFrom(client.GetStream());
		if(response.Result.ValueCase == Result.ValueOneofCase.Ok)
		{
			RoomWindow w = new(address: ServerAddressBox.Text, client: client)
			{
				WindowState = WindowState,
			};
			if(!w.closed)
			{
				w.Show();
				Close();
			}
		}
		else
		{
			_ = new ErrorPopup(response.Result.Error?.Reason ?? "No error reason provided").ShowDialog(this);
		}
	}
	void RefreshClick(object? sender, RoutedEventArgs args)
	{
		UpdateRoomList();
	}
	private void JoinClick(object? sender, RoutedEventArgs args)
	{
		if(sender is null || ServerAddressBox.Text == null || PlayerNameBox.Text == null || string.IsNullOrEmpty(PlayerNameBox.Text))
		{
			return;
		}
		string targetNameText = (string)((Button)sender).Content!;
		TcpClient client = new(ServerAddressBox.Text, GenericConstants.SERVER_PORT);
		new CardGameUtils.ServerClientToServer.Packet
		{
			Join = new()
			{
				OwnName = PlayerNameBox.Text,
				OppName = targetNameText
			}
		}.WriteDelimitedTo(client.GetStream());
		CardGameUtils.ServerServerToClient.Join response = CardGameUtils.ServerServerToClient.Join.Parser.ParseDelimitedFrom(client.GetStream());
		if(response.Result.ValueCase == Result.ValueOneofCase.Ok)
		{
			new RoomWindow(ServerAddressBox.Text, client, opponentName: targetNameText)
			{
				WindowState = WindowState,
			}.Show();
			Close();
		}
		else
		{
			_ = new ErrorPopup(response.Result.Error?.Reason ?? "No error reason provided").ShowDialog(this);
		}
	}
}
public class ServerWindowViewModel : INotifyPropertyChanged
{
	public ServerWindowViewModel()
	{
		PlayerName ??= Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.Now.Millisecond + DateTime.Now.ToLongTimeString()));
		Program.config.server_address ??= "127.0.0.1";
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public string ServerAddress
	{
		get
		{
			Program.config.server_address ??= "127.0.0.1";
			return Program.config.server_address;
		}
		set
		{
			if(value != Program.config.server_address)
			{
				Program.config.server_address = value;
				NotifyPropertyChanged();
			}
		}
	}

	public string PlayerName
	{
		get
		{
			Program.config.player_name ??= Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.Now.Millisecond + DateTime.Now.ToLongTimeString()));
			return Program.config.player_name;
		}
		set
		{
			if(value != Program.config.player_name)
			{
				Program.config.player_name = value;
				NotifyPropertyChanged();
			}
		}
	}
	private string[] serverRooms = [];
	public string[] ServerRooms
	{
		get => serverRooms;
		set
		{
			if(value != serverRooms)
			{
				serverRooms = value;
				NotifyPropertyChanged();
			}
		}
	}
}
