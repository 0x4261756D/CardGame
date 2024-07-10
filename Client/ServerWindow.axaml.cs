using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.Packets.Server;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;
using Avalonia.Threading;

namespace CardGameClient;

public partial class ServerWindow : Window
{
	public ServerWindow()
	{
		DataContext = new ServerWindowViewModel();
		InitializeComponent();
		Functions.Log("Before UpdateRoomList");
		Dispatcher.UIThread.Invoke(async () => await UpdateRoomList());
		Functions.Log("after UpdateRoomList");
	}
	public void BackClick(object sender, RoutedEventArgs args)
	{
		new MainWindow
		{
			WindowState = WindowState,
		}.Show();
		Close();
	}
	private async Task UpdateRoomList()
	{
		if(ServerAddressBox.Text == null)
		{
			new ErrorPopup("Connection to the server timed out").Show();
			return;
		}
		try
		{
			Functions.Log("Before sending updateroomlist");
			TTransport transport = new TSocketTransport(host: ServerAddressBox.Text!, port: GenericConstants.SERVER_PORT, timeout: 1000, config: new());
			await new ClientPacket.rooms(new()).WriteAsync(new TCompactProtocol(transport), default);
			Functions.Log("after sending updateroomlist");
			transport.CheckReadBytesAvailable(1);
			Functions.Log("Bytes are available");
			ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
			((ServerWindowViewModel)DataContext!).ServerRooms = packet.As_rooms!.Rooms!;
			Functions.Log("after receiving roomlist");
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
	private async void HostClick(object? sender, RoutedEventArgs args)
	{
		if(ServerAddressBox.Text == null)
		{
			return;
		}
		string playerName = ((ServerWindowViewModel)DataContext!).PlayerName;
		TTransport transport;
		TcpClient client;
		try
		{
			client = new(ServerAddressBox.Text, GenericConstants.SERVER_PORT);
			transport = new TSocketTransport(client, new());
		}
		catch(Exception ex)
		{
			if(IsVisible)
			{
				new ErrorPopup(ex.Message).Show();
			}
			return;
		}
		await new ClientPacket.create(new() { Name = playerName }).WriteAsync(new TCompactProtocol(transport), default);
		ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		Result response = packet.As_create!.Result!;
		if(response is Result.success)
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
			_ = new ErrorPopup(response.As_failure!.Result!).ShowDialog(this);
		}
	}
	private async void RefreshClick(object? sender, RoutedEventArgs args)
	{
		await UpdateRoomList();
	}
	private async void JoinClick(object? sender, RoutedEventArgs args)
	{
		if(sender is null || ServerAddressBox.Text == null || PlayerNameBox.Text == null || string.IsNullOrEmpty(PlayerNameBox.Text))
		{
			return;
		}
		string targetNameText = (string)((Button)sender!).Content!;
		TcpClient client = new(ServerAddressBox.Text, GenericConstants.SERVER_PORT);
		TTransport transport = new TSocketTransport(client, new());
		await new ClientPacket.join(new() { Own_name = PlayerNameBox.Text, Opp_name = targetNameText }).WriteAsync(new TCompactProtocol(transport), default);
		ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		Result response = packet.As_join!.Result!;
		if(response is Result.success)
		{
			new RoomWindow(ServerAddressBox.Text, client, opponentName: targetNameText)
			{
				WindowState = WindowState,
			}.Show();
			Close();
		}
		else
		{
			_ = new ErrorPopup(response.As_failure!.Result!).ShowDialog(this);
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
	private List<string> serverRooms = [];
	public List<string> ServerRooms
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
