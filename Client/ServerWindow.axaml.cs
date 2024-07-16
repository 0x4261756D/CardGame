using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.Packets.Server;
using Google.FlatBuffers;
using CardGameUtils.Constants;

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
			using TcpClient updateClient = new(ServerAddressBox.Text, InternalConstants.SERVER_PORT);
			using NetworkStream updateStream = updateClient.GetStream();
			updateStream.Write(ClientPacketTToByteArray(new(){Content = new(){Type = ClientContent.rooms, Value = new ClientRoomsPacketT()}}));
			ServerPacket packet = Functions.ReadSizedServerServerPacketFromStream(updateStream);
			if(packet.ContentType != ServerContent.rooms)
			{
				throw new Exception($"Expected packet of type `rooms` but got {packet.ContentType}");
			}

			((ServerWindowViewModel)DataContext!).ServerRooms = [.. packet.ContentAsrooms().UnPack().Rooms];
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
			client = new(ServerAddressBox.Text, InternalConstants.SERVER_PORT);
		}
		catch(Exception ex)
		{
			if(IsVisible)
			{
				new ErrorPopup(ex.Message).Show();
			}
			return;
		}
		client.GetStream().Write(ClientPacketTToByteArray(new(){Content = new(){Type = ClientContent.create, Value = new ClientCreatePacketT {Name = playerName}}}));
		ServerPacket packet = Functions.ReadSizedServerServerPacketFromStream(client.GetStream());
		client.Close();
		if(packet.ContentType != ServerContent.create)
		{
			throw new Exception($"Expected packet of type `create` but got {packet.ContentType}");
		}
		ServerCreatePacket response = packet.ContentAscreate();
		if(response.ResultType == Result.ResultSuccess)
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
			_ = new ErrorPopup(response.ResultAsResultFailure().Reason).ShowDialog(this);
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
		TcpClient client = new(ServerAddressBox.Text, InternalConstants.SERVER_PORT);
		client.GetStream().Write(ClientPacketTToByteArray(new(){Content = new(){Type = ClientContent.join, Value = new ClientJoinPacketT
		{
			OppName = targetNameText,
			OwnName = PlayerNameBox.Text,
		}}}));
		ServerPacket packet = Functions.ReadSizedServerServerPacketFromStream(client.GetStream());
		if(packet.ContentType != ServerContent.join)
		{
			throw new Exception($"Expected packet of type `join` but got {packet.ContentType}");
		}
		ServerJoinPacket response = packet.ContentAsjoin();
		if(response.ResultType == Result.ResultSuccess)
		{
			new RoomWindow(ServerAddressBox.Text, client, opponentName: targetNameText)
			{
				WindowState = WindowState,
			}.Show();
			Close();
		}
		else
		{
			_ = new ErrorPopup(response.ResultAsResultFailure().Reason).ShowDialog(this);
		}
	}
	public static byte[] ClientPacketTToByteArray(ClientPacketT packet)
	{
		FlatBufferBuilder builder = new(1);
		builder.FinishSizePrefixed(ClientPacket.Pack(builder, packet).Value);
		return builder.DataBuffer.ToSizedArray();
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
