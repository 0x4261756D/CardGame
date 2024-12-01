using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils.Structs.Duel;
using System.Threading;

namespace CardGameClient;
public partial class YesNoWindow : Window
{
	readonly Stream stream;
	private bool shouldReallyClose;
	public YesNoWindow(string description, Stream stream, Mutex streamMutex)
	{
		InitializeComponent();
		MessageBlock.Text = description;
		this.stream = stream;
		_ = streamMutex.WaitOne();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		Closing += (sender, args) =>
		{
			args.Cancel = !shouldReallyClose;
		};
		Closed += (_, _) => streamMutex.ReleaseMutex();
	}

	public void YesClick(object? sender, RoutedEventArgs args)
	{
		stream.Write(new CToS_Packet(new CToS_Content.yes_no(new(yes: true))).Serialize());
		shouldReallyClose = true;
		Close();
	}

	public void NoClick(object? sender, RoutedEventArgs args)
	{
		stream.Write(new CToS_Packet(new CToS_Content.yes_no(new(yes: false))).Serialize());
		shouldReallyClose = true;
		Close();
	}
}
