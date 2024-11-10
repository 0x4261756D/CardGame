using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.Structs.Server;

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
			updateStream.Write(new CToS_Packet(new CToS_Content.rooms()).Deserialize());
			((ServerWindowViewModel)DataContext!).ServerRooms = [.. ReceivePacket<SToC_Content.rooms>(updateStream).value.rooms];
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
		client.GetStream().Write(new CToS_Packet(new CToS_Content.create(new(name: playerName))).Deserialize());
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
		TcpClient client = new(ServerAddressBox.Text, GenericConstants.SERVER_PORT);
		client.GetStream().Write(new CToS_Packet(new CToS_Content.join(new
		(
			own_name: PlayerNameBox.Text,
			opp_name: targetNameText
		))).Deserialize());
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
		stream.Write(new CToS_Packet(content).Deserialize());
		return (T)SToC_Packet.Serialize(stream).content;
	}
	public static T ReceivePacket<T>(NetworkStream stream) where T : SToC_Content
	{
		return (T)SToC_Packet.Serialize(stream).content;
	}
	public static SToC_Content? TryReceivePacket(NetworkStream stream, int timeoutInMs)
	{
		try
		{
			Task<SToC_Packet> task = Task.Run(() => SToC_Packet.Serialize(stream));
			int i = Task.WaitAny(task, Task.Delay(timeoutInMs));
			return i == 0 ? task.Result.content : null;
		}
		catch(Exception e)
		{
			Functions.Log(e.Message, severity: Functions.LogSeverity.Warning);
			return null;
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
