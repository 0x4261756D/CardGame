using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.Packets.Duel;
using Thrift.Protocol;

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

	public async void ToEndClick(object sender, RoutedEventArgs args)
	{
		if(replay != null && window != null)
		{
			for(; actionIndex < replay.Actions?.Count; actionIndex++)
			{
				GameAction action = replay.Actions[actionIndex];
				ReplayPacket packet = action.Packet!;
				((ReplaysViewModel)DataContext!).ActionList.Insert
				(
					0,
					$"{(IsFieldUpdateForCurrentPlayer(packet, action.Player) ? "*" : "")}{actionIndex}: Player {action.Player}: {(action.Packet is ReplayPacket.server ? "<-" : "->")} {packet.GetType()}"
				);
			}
			GameAction lastAction = replay.Actions?.FindLast((action) => IsFieldUpdateForCurrentPlayer(action.Packet!, action.Player))!;
			window.EnqueueFieldUpdate(lastAction.Packet!.As_server!.As_field_update!);
			await window.UpdateField();
		}
	}

	private bool IsFieldUpdateForCurrentPlayer(ReplayPacket packet, int player)
	{
		return player == (((ReplaysViewModel)DataContext!).IsFirstPlayer ? 0 : 1) && packet is ReplayPacket.server && packet.As_server is ServerPacket.field_update;
	}

	public async void StartClick(object sender, RoutedEventArgs args)
	{
		if(!File.Exists(FilePathBox.Text))
		{
			new ErrorPopup($"Replay {FilePathBox.Text} does not exist").Show();
			return;
		}
		replay = new();
		await replay.ReadAsync(new TJsonProtocol(new Functions.TSimpleFileTransport(FilePathBox.Text, Functions.TSimpleFileTransport.OpenMode.Read)), default);
		actionIndex = 0;
		((ReplaysViewModel)DataContext!).ActionList.Clear();
		window?.Close();
		window = new DuelWindow();
		window.Show(this);
		await Next();
	}
	public async Task Next()
	{
		if(replay is null || window is null || replay.Actions is null || actionIndex >= replay.Actions.Count - 1)
		{
			return;
		}
		GameAction action = replay.Actions[actionIndex];
		ReplayPacket packet = action.Packet!;
		while(!IsFieldUpdateForCurrentPlayer(packet, action.Player))
		{
			((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"{actionIndex}: Player {action.Player}: {(action.Packet is ReplayPacket.server ? "<-" : "->")} {packet.GetType()}");
			actionIndex++;
			if(actionIndex >= replay.Actions.Count - 1)
			{
				return;
			}
			action = replay.Actions[actionIndex];
			packet = action.Packet!;
		}
		((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"* {actionIndex}: Player {action.Player}: {(packet is ReplayPacket.server ? "<-" : "->")} {packet.GetType()}");
		window.EnqueueFieldUpdate(packet.As_server!.As_field_update!);
		await window.UpdateField();
		actionIndex++;
	}

	public async Task Prev()
	{
		if(replay == null || window == null || actionIndex < 2)
		{
			return;
		}
		actionIndex -= 2;
		((ReplaysViewModel)DataContext!).ActionList.RemoveAt(0);
		GameAction action = replay.Actions![actionIndex];
		ReplayPacket packet = action.Packet!;
		while(!IsFieldUpdateForCurrentPlayer(packet, action.Player))
		{
			((ReplaysViewModel)DataContext!).ActionList.RemoveAt(0);
			if(packet is ReplayPacket.server && packet.As_server is ServerPacket.game_result)
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
			action = replay.Actions[actionIndex];
			packet = action.Packet!;
		}
		window.EnqueueFieldUpdate(packet.As_server!.As_field_update!);
		await window.UpdateField();
		actionIndex++;
	}

	public async void NextClick(object sender, RoutedEventArgs args)
	{
		await Next();
	}

	public async void PrevClick(object sender, RoutedEventArgs args)
	{
		await Prev();
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
