using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.Structs.Server;

namespace CardGameClient;

internal partial class ServerWindow : Window
{
	public ServerWindow()
	{
		Program.config.player_name ??= Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.Now.Millisecond + DateTime.Now.ToLongTimeString()));
		Program.config.server_address ??= "127.0.0.1";
		InitializeComponent();
		PlayerNameBox.Text = Program.config.player_name;
		ServerAddressBox.Text = Program.config.server_address;
		UpdateRoomList();
	}
	public void BackClick(object sender, RoutedEventArgs args)
	{
		Program.config.player_name = PlayerNameBox.Text;
		Program.config.server_address = ServerAddressBox.Text ?? "127.0.0.1";
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
			using TcpClient updateClient = new(ServerAddressBox.Text, GameConstants.SERVER_PORT);
			using NetworkStream updateStream = updateClient.GetStream();
			updateStream.Write(new CToS_Packet(new CToS_Content.rooms()).Serialize());
			ServerListBox.Items.Clear();
			foreach(string roomName in ReceivePacket<SToC_Content.rooms>(updateStream).value.rooms)
			{
				ServerListBox.Items.Add(roomName);
			}
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
		Program.config.player_name = PlayerNameBox.Text ?? Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.Now.Millisecond + DateTime.Now.ToLongTimeString()));
		Program.config.server_address = ServerAddressBox.Text;
		TcpClient client;
		try
		{
			client = new(ServerAddressBox.Text, GameConstants.SERVER_PORT);
		}
		catch(Exception ex)
		{
			if(IsVisible)
			{
				new ErrorPopup(ex.Message).Show();
			}
			return;
		}
		client.GetStream().Write(new CToS_Packet(new CToS_Content.create(new(name: Program.config.player_name))).Serialize());
		ErrorOr response = ReceivePacket<SToC_Content.create>(client.GetStream()).value.success;
		switch(response)
		{
			case ErrorOr.success:
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
			break;
			case ErrorOr.error error:
			{
				_ = new ErrorPopup(error.value).ShowDialog(this);
			}
			break;
			default:
				throw new NotImplementedException();
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
		TcpClient client = new(ServerAddressBox.Text, GameConstants.SERVER_PORT);
		client.GetStream().Write(new CToS_Packet(new CToS_Content.join(new
		(
			own_name: PlayerNameBox.Text,
			opp_name: targetNameText
		))).Serialize());
		ErrorOr response = ReceivePacket<SToC_Content.join>(client.GetStream()).value.success;
		switch(response)
		{
			case ErrorOr.success:
			{
				new RoomWindow(ServerAddressBox.Text, client, opponentName: targetNameText)
				{
					WindowState = WindowState,
				}.Show();
				Close();
			}
			break;
			case ErrorOr.error error:
			{
				_ = new ErrorPopup(error.value).ShowDialog(this);
			}
			break;
		}
	}
	public static T? TrySendAndReceive<T>(CToS_Content content, string address, int port) where T : SToC_Content
	{
		try
		{
			return SendAndReceive<T>(content, address, port);
		}
		catch(Exception ex)
		{
			new ErrorPopup(ex.Message).Show();
			return default;
		}
	}
	public static T SendAndReceive<T>(CToS_Content content, string address, int port) where T : SToC_Content
	{
		using TcpClient client = new(address, port);
		using NetworkStream stream = client.GetStream();
		stream.Write(new CToS_Packet(content).Serialize());
		return (T)SToC_Packet.Deserialize(stream).content;
	}
	public static T ReceivePacket<T>(NetworkStream stream) where T : SToC_Content
	{
		return (T)SToC_Packet.Deserialize(stream).content;
	}
	public static SToC_Content? TryReceivePacket(NetworkStream stream, int timeoutInMs)
	{
		try
		{
			Task task = Task.Run(() => { while(!stream.DataAvailable) { } return; });
			int i = Task.WaitAny(task, Task.Delay(timeoutInMs));
			return i == 0 ? SToC_Packet.Deserialize(stream).content : null;
		}
		catch(Exception e)
		{
			Functions.Log(e.Message, severity: Functions.LogSeverity.Warning);
			return null;
		}
	}
}
