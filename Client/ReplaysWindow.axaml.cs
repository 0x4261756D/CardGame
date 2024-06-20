using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;

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
				Replay.IPacket packet = action.PacketContentToPacket();
				((ReplaysViewModel)DataContext!).ActionList.Insert
				(
					0,
					$"{(IsFieldUpdateForCurrentPlayer(action.player, packet) ? "*" : "")}{actionIndex}: Player {action.player}: {(action.isClientToServer ? "<-" : "->")} {packet.GetType()}"
				);
			}
			Replay.GameAction lastAction = replay.actions.FindLast((action) => action.isClientToServer && action.player == (((ReplaysViewModel)DataContext!).IsFirstPlayer ? 0 : 1))!;

			window.EnqueueFieldUpdate(((Replay.StoC)lastAction.PacketContentToPacket()).packet.FieldUpdate);
			window.UpdateField();
		}
	}

	private bool IsFieldUpdateForCurrentPlayer(int player, Replay.IPacket packet)
	{
		return player == (((ReplaysViewModel)DataContext!).IsFirstPlayer ? 0 : 1) && packet is Replay.CtoS;
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
		Replay.IPacket packet = action.PacketContentToPacket();
		while(!IsFieldUpdateForCurrentPlayer(action.player, packet))
		{
			((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"{actionIndex}: Player {action.player}: {(action.isClientToServer ? "<-" : "->")} {packet.GetType()}");
			actionIndex++;
			if(actionIndex >= replay.actions.Count - 1)
			{
				return;
			}
			action = replay.actions[actionIndex];
			packet = action.PacketContentToPacket();
		}
		((ReplaysViewModel)DataContext!).ActionList.Insert(0, $"* {actionIndex}: Player {action.player}: {(action.isClientToServer ? "<-" : "->")} {packet.GetType()}");
		window.EnqueueFieldUpdate(((Replay.StoC)packet).packet.FieldUpdate);
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
		Replay.IPacket packet = action.PacketContentToPacket();
		while(!IsFieldUpdateForCurrentPlayer(action.player, packet))
		{
			((ReplaysViewModel)DataContext!).ActionList.RemoveAt(0);
			if(packet is Replay.StoC stoc && stoc.packet.KindCase == CardGameUtils.DuelServerToClient.Packet.KindOneofCase.GameResult)
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
			packet = action.PacketContentToPacket();
		}
		window.EnqueueFieldUpdate(((Replay.StoC)packet).packet.FieldUpdate);
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
