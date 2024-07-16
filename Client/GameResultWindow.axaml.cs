using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;
using CardGameUtils.Packets.Duel;

namespace CardGameClient;

public partial class GameResultWindow : Window
{
	readonly Window parent;

	public GameResultWindow(Window parent, ServerGameResultPacket response)
	{
		this.parent = parent;
		InitializeComponent();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		Closed += (_, _) =>
		{
			if(this.parent.IsEnabled)
			{
				this.parent.Close();
			}
		};
		ResultBlock.Text = (response.Result == GameResult.Draw) ? "It was a draw" : $"You {response.Result}";
		Topmost = true;
	}
	public void BackClick(object? sender, RoutedEventArgs args)
	{
		new ServerWindow
		{
			WindowState = WindowState,
		}.Show();
		Close();
	}
}
