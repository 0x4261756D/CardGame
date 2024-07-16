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
using CardGameUtils.Packets.Duel;
using CardGameUtils.Constants;
using Google.FlatBuffers;

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
			ReplayAction? lastAction = null;
			for(; actionIndex < replay.Value.ActionsLength; actionIndex++)
			{
				ReplayAction action = replay.Value.Actions(actionIndex)!.Value;
				((ReplaysViewModel)DataContext!).ActionList.Insert
				(
					0,
					$"{(IsFieldUpdateForCurrentPlayer(action) ? "*" : "")}{actionIndex}: Player {action.Player}: {((action.PacketType == ReplayActionPacket.server) ? "<-" : "->")} {((action.PacketType == ReplayActionPacket.client) ? action.PacketAsclient().ContentType : action.PacketAsserver().ContentType)}"
				);
				if(IsFieldUpdateForCurrentPlayer(action))
				{
					lastAction = action;
				}
			}
			if(lastAction is not null)
			{
				window.EnqueueFieldUpdate(lastAction.Value.PacketAsserver().ContentAsfieldupdate());
				window.UpdateField();
			}
		}
	}

	private bool IsFieldUpdateForCurrentPlayer(ReplayAction action)
	{
		return action.Player == (((ReplaysViewModel)DataContext!).IsFirstPlayer ? 0 : 1) && action.PacketType == ReplayActionPacket.server && action.PacketAsserver().ContentType == ServerContent.fieldupdate;
	}

	public void StartClick(object sender, RoutedEventArgs args)
	{
		if(!File.Exists(FilePathBox.Text))
		{
			new ErrorPopup($"Replay {FilePathBox.Text} does not exist").Show();
			return;
		}
		replay = Replay.GetRootAsReplay(new ByteBuffer(File.ReadAllBytes(FilePathBox.Text)));
		actionIndex = 0;
		((ReplaysViewModel)DataContext!).ActionList.Clear();
		window?.Close();
		window = new DuelWindow();
		window.Show(this);
		Next();
	}
	public void Next()
	{
		if(replay == null || window == null || actionIndex >= replay.Value.ActionsLength - 1)
		{
			return;
		}
		ReplayAction action = replay.Value.Actions(actionIndex)!.Value;
		while(!IsFieldUpdateForCurrentPlayer(action))
		{
			((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"{actionIndex}: Player {action.Player}: {((action.PacketType == ReplayActionPacket.server) ? "<-" : "->")} {((action.PacketType == ReplayActionPacket.client) ? action.PacketAsclient().ContentType : action.PacketAsserver().ContentType)}");
			actionIndex++;
			if(actionIndex >= replay.Value.ActionsLength - 1)
			{
				return;
			}
			action = replay.Value.Actions(actionIndex)!.Value;
		}
		((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"* {actionIndex}: Player {action.Player}: {((action.PacketType == ReplayActionPacket.server) ? "<-" : "->")} {((action.PacketType == ReplayActionPacket.client) ? action.PacketAsclient().ContentType : action.PacketAsserver().ContentType)}");
		window.EnqueueFieldUpdate(action.PacketAsserver().ContentAsfieldupdate());
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
		ReplayAction action = replay.Value.Actions(actionIndex)!.Value;
		while(!IsFieldUpdateForCurrentPlayer(action))
		{
			((ReplaysViewModel)DataContext!).ActionList.RemoveAt(0);
			if(action.PacketType == ReplayActionPacket.server && action.PacketAsserver().ContentType == ServerContent.gameresult)
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
			action = replay.Value.Actions(actionIndex)!.Value;
		}
		window.EnqueueFieldUpdate(action.PacketAsserver().ContentAsfieldupdate());
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
