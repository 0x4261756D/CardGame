using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Google.Protobuf;

namespace CardGameClient;
public partial class YesNoWindow : Window
{
	readonly Stream stream;
	private bool shouldReallyClose;
	public YesNoWindow(string description, Stream stream)
	{
		InitializeComponent();
		MessageBlock.Text = description;
		this.stream = stream;
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		Closing += (sender, args) =>
		{
			args.Cancel = !shouldReallyClose;
		};
	}

	public void YesClick(object? sender, RoutedEventArgs args)
	{
		new CardGameUtils.DuelClientToServer.YesNo { Result = true }.WriteDelimitedTo(stream);
		shouldReallyClose = true;
		Close();
	}

	public void NoClick(object? sender, RoutedEventArgs args)
	{
		new CardGameUtils.DuelClientToServer.YesNo { Result = false }.WriteDelimitedTo(stream);
		shouldReallyClose = true;
		Close();
	}
}
