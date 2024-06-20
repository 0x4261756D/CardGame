using System.IO;
using Avalonia.Controls;
using Google.Protobuf;

namespace CardGameClient;

public partial class SelectZoneWindow : Window
{
	private bool shouldReallyClose;

	public SelectZoneWindow(bool[] options, Stream stream)
	{
		InitializeComponent();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		for(int i = 0; i < options.Length; i++)
		{
			Button b = new()
			{
				Content = i
			};
			b.Click += (sender, _) =>
			{
				int zone = (int)((Button)sender!).Content!;
				new CardGameUtils.DuelClientToServer.Packet
				{
					SelectZone = new()
					{
						Zone = zone,
					}
				}.WriteDelimitedTo(stream);
				shouldReallyClose = true;
				Close();
			};
			b.IsEnabled = options[i];
			OptionsPanel.Children.Add(b);
		}
		Closing += (_, args) =>
		{
			args.Cancel = !shouldReallyClose;
		};
	}
}
