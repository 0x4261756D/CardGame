using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils.Replay;
using CardGameUtils.Structs.Duel;

namespace CardGameClient;

internal partial class ReplaysWindow : Window
{
	private DuelWindow? window;
	private Replay? replay;
	private int actionIndex;

	public ReplaysWindow()
	{
		InitializeComponent();
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
				ActionListBox.Items.Insert
				(
					0,
					$"{(IsFieldUpdateForCurrentPlayer(action) ? "*" : "")}{actionIndex}: Player {action.player}: {(action.content is ReplayContent.ctos ? "<-" : "->")} {(action.content is ReplayContent.ctos ctos ? ctos.value.GetType() : ((ReplayContent.stoc)action.content).value.GetType())}"
				);
			}
			EnqueueFieldUpdateOrInfo(((ReplayContent.stoc)replay.packets.FindLast(IsFieldUpdateForCurrentPlayer)!.content).value);
			window.UpdateField();
		}
	}

	private bool IsFieldUpdateForCurrentPlayer(ReplayPacket packet)
	{
		return packet.player == (FirstPlayerBox.IsChecked!.Value ? 0 : 1) && packet.content is ReplayContent.stoc stoc && (stoc.value is SToC_Content.field_update || stoc.value is SToC_Content.show_info);
	}

	public void StartClick(object sender, RoutedEventArgs args)
	{
		if(!File.Exists(FilePathBox.Text))
		{
			new ErrorPopup($"Replay {FilePathBox.Text} does not exist").Show();
			return;
		}
		replay = Replay.Deserialize(File.ReadAllBytes(FilePathBox.Text));
		if(replay is null)
		{
			new ErrorPopup($"Could not open replay {FilePathBox.Text}").Show();
			return;
		}
		actionIndex = 0;
		ActionListBox.Items.Clear();
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
			{
				ActionListBox.Items.Insert(0, $"  {actionIndex}: Player {action.player}: {(action.content is ReplayContent.ctos ? "<-" : "->")} {(action.content is ReplayContent.ctos ctos ? ctos.value.GetType() : ((ReplayContent.stoc)action.content).value.GetType())}");
			}
			actionIndex++;
			if(actionIndex >= replay.packets.Count - 1)
			{
				return;
			}
			action = replay.packets[actionIndex];
		}
		{
			ActionListBox.Items.Insert(0, $"* {actionIndex}: Player {action.player}: {(action.content is ReplayContent.ctos ? "<-" : "->")} {(action.content is ReplayContent.ctos ctos ? ctos.value.GetType() : ((ReplayContent.stoc)action.content).value.GetType())}");
		}
		EnqueueFieldUpdateOrInfo(((ReplayContent.stoc)action.content).value);
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
		ActionListBox.Items.RemoveAt(0);
		ReplayPacket action = replay.packets[actionIndex];
		while(!IsFieldUpdateForCurrentPlayer(action))
		{
			ActionListBox.Items.RemoveAt(0);
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
		EnqueueFieldUpdateOrInfo(((ReplayContent.stoc)action.content).value);
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

	public void EnqueueFieldUpdateOrInfo(SToC_Content content)
	{
		if(content is SToC_Content.field_update fieldUpdate)
		{
			window!.EnqueueFieldUpdateOrInfo(new DuelWindow.IFieldUpdateOrInfo.FieldUpdate(fieldUpdate.value));
		}
		else
		{
			window!.EnqueueFieldUpdateOrInfo(new DuelWindow.IFieldUpdateOrInfo.Info(((SToC_Content.show_info)content).value));
		}

	}
}
