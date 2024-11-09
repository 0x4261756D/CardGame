using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils.Replay;
using CardGameUtils.Structs.Duel;

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
			for(; actionIndex < replay.packets.Count; actionIndex++)
			{
				ReplayPacket action = replay.packets[actionIndex];
				((ReplaysViewModel)DataContext!).ActionList.Insert
				(
					0,
					$"{(IsFieldUpdateForCurrentPlayer(action) ? "*" : "")}{actionIndex}: Player {action.player}: {(action.content is ReplayContent.ctos ? "<-" : "->")} {action.content.GetType()}"
				);
			}
			SToC_Broadcast_FieldUpdate lastAction = ((SToC_Content.field_update)((ReplayContent.stoc)replay.packets.FindLast(IsFieldUpdateForCurrentPlayer)!.content).value).value;
			window.EnqueueFieldUpdate(lastAction);
			window.UpdateField();
		}
	}

	private bool IsFieldUpdateForCurrentPlayer(ReplayPacket packet)
	{
		return packet.player == (((ReplaysViewModel)DataContext!).IsFirstPlayer ? 0 : 1) && packet.content is ReplayContent.stoc stoc && stoc.value is SToC_Content.field_update;
	}

	public void StartClick(object sender, RoutedEventArgs args)
	{
		if(!File.Exists(FilePathBox.Text))
		{
			new ErrorPopup($"Replay {FilePathBox.Text} does not exist").Show();
			return;
		}
		replay = Replay.Serialize(File.ReadAllBytes(FilePathBox.Text));
		if(replay is null)
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
		if(replay == null || window == null || actionIndex >= replay.packets.Count - 1)
		{
			return;
		}
		ReplayPacket action = replay.packets[actionIndex];
		while(!IsFieldUpdateForCurrentPlayer(action))
		{
			((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"  {actionIndex}: Player {action.player}: {(action.content is ReplayContent.ctos ? "<-" : "->")} {action.content.GetType()}");
			actionIndex++;
			if(actionIndex >= replay.packets.Count - 1)
			{
				return;
			}
			action = replay.packets[actionIndex];
		}
		((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"* {actionIndex}: Player {action.player}: {(action.content is ReplayContent.ctos ? "<-" : "->")} {action.content.GetType()}");
		window.EnqueueFieldUpdate(((SToC_Content.field_update)((ReplayContent.stoc)action.content).value).value);
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
		ReplayPacket action = replay.packets[actionIndex];
		while(!IsFieldUpdateForCurrentPlayer(action))
		{
			((ReplaysViewModel)DataContext!).ActionList.RemoveAt(0);
			if(action.content is ReplayContent.stoc stoc && stoc.value is SToC_Content.game_result)
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
			action = replay.packets[actionIndex];
		}
		window.EnqueueFieldUpdate(((SToC_Content.field_update)((ReplayContent.stoc)action.content).value).value);
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
