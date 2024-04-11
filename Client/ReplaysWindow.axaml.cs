using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;

namespace CardGameClient;

public partial class ReplaysWindow : Window
{
	private DuelWindow? window;
	private Replay? replay;
	private int actionIndex;

	public ReplaysWindow()
	{
		InitializeComponent();
		DataContext = new ReplaysViewModel();
		Width = Program.config.width / 5;
		Topmost = true;
	}
	public void BackClick(object sender, RoutedEventArgs args)
	{
		new MainWindow
		{
			WindowState = WindowState,
		}.Show();
		Close();
	}

	public async void SelectFileClick(object sender, RoutedEventArgs args)
	{
		FilePathBox.Text = await UIUtils.SelectFileAsync(this, "Select Replay").ConfigureAwait(true);
	}

	public void ToEndClick(object sender, RoutedEventArgs args)
	{
		if(replay != null && window != null)
		{
			for(; actionIndex < replay.actions.Count; actionIndex++)
			{
				Replay.GameAction action = replay.actions[actionIndex];
				NetworkingStructs.Packet packet = DeserializeRaw(action.PacketContentBytes());
				((ReplaysViewModel)DataContext!).ActionList.Insert
				(
					0,
					$"{(IsFieldUpdateForCurrentPlayer(action.clientToServer, action.player, packet) ? "*" : "")}{actionIndex}: Player {action.player}: {(action.clientToServer ? "<-" : "->")} {packet.GetType()}"
				);
			}
			Replay.GameAction lastAction = replay.actions.FindLast((action) => IsFieldUpdateForCurrentPlayer(action.clientToServer, action.player, DeserializeRaw(action.PacketContentBytes())))!;
			window.EnqueueFieldUpdate((NetworkingStructs.DuelPackets.FieldUpdateRequest)DeserializeRaw(lastAction.PacketContentBytes()));
			window.UpdateField();
		}
	}

	private bool IsFieldUpdateForCurrentPlayer(bool clientToServer, int player, NetworkingStructs.Packet packet)
	{
		return !clientToServer && player == (((ReplaysViewModel)DataContext!).IsFirstPlayer ? 0 : 1) && packet is NetworkingStructs.DuelPackets.FieldUpdateRequest;
	}

	public void StartClick(object sender, RoutedEventArgs args)
	{
		if(!File.Exists(FilePathBox.Text))
		{
			new ErrorPopup($"Replay {FilePathBox.Text} does not exist").Show();
			return;
		}
		string text = File.ReadAllText(FilePathBox.Text);
		replay = JsonSerializer.Deserialize<Replay>(text, GenericConstants.replaySerialization);
		if(replay == null)
		{
			new ErrorPopup($"Could not open replay {FilePathBox.Text}").Show();
			return;
		}
		actionIndex = 0;
		((ReplaysViewModel)DataContext!).ActionList.Clear();
		window?.Close();
		window = new DuelWindow();
		window.Show(this);
		Next();
	}
	public void Next()
	{
		if(replay == null || window == null || actionIndex >= replay.actions.Count - 1)
		{
			return;
		}
		Replay.GameAction action = replay.actions[actionIndex];
		if(action.packetVersion != GenericConstants.PACKET_VERSION)
		{
			new ErrorPopup($"Replay was made by an incompatble game version (expected {GenericConstants.PACKET_VERSION}, replay has {action.packetVersion})").Show();
		}
		NetworkingStructs.Packet packet = DeserializeRaw(action.PacketContentBytes());
		while(!IsFieldUpdateForCurrentPlayer(action.clientToServer, action.player, packet))
		{
			((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"{actionIndex}: Player {action.player}: {(action.clientToServer ? "<-" : "->")} {packet.GetType()}");
			actionIndex++;
			if(actionIndex >= replay.actions.Count - 1)
			{
				return;
			}
			action = replay.actions[actionIndex];
			packet = DeserializeRaw(action.PacketContentBytes());
		}
		((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"* {actionIndex}: Player {action.player}: {(action.clientToServer ? "<-" : "->")} {packet.GetType()}");
		window.EnqueueFieldUpdate((NetworkingStructs.DuelPackets.FieldUpdateRequest)packet);
		window.UpdateField();
		actionIndex++;
	}

	public void Prev()
	{
		if(replay == null || window == null || actionIndex < 2)
		{
			return;
		}
		actionIndex -= 2;
		((ReplaysViewModel)DataContext!).ActionList.RemoveAt(0);
		Replay.GameAction action = replay.actions[actionIndex];
		NetworkingStructs.Packet packet = DeserializeRaw(action.PacketContentBytes());
		while(!IsFieldUpdateForCurrentPlayer(action.clientToServer, action.player, packet))
		{
			((ReplaysViewModel)DataContext!).ActionList.RemoveAt(0);
			if(packet is NetworkingStructs.DuelPackets.GameResultResponse)
			{
				window.Close();
				return;
			}
			actionIndex--;
			if(actionIndex < 0)
			{
				actionIndex = 0;
				window.Close();
				return;
			}
			action = replay.actions[actionIndex];
			packet = DeserializeRaw(action.PacketContentBytes());
		}
		window.EnqueueFieldUpdate((NetworkingStructs.DuelPackets.FieldUpdateRequest)packet);
		window.UpdateField();
		actionIndex++;
	}

	public void NextClick(object sender, RoutedEventArgs args)
	{
		Next();
	}

	public void PrevClick(object sender, RoutedEventArgs args)
	{
		Prev();
	}
}

public class ReplaysViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool isFirstPlayer = true;
	public bool IsFirstPlayer
	{
		get => isFirstPlayer;
		set
		{
			if(isFirstPlayer != value)
			{
				isFirstPlayer = value;
				NotifyPropertyChanged();
			}
		}
	}

	private readonly ObservableCollection<string> actionList = [];
	public ObservableCollection<string> ActionList
	{
		get => actionList;
	}
}
