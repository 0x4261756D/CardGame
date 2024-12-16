using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using CardGameUtils.Structs.Duel;
using System.Threading;

namespace CardGameClient;

internal partial class SelectZoneWindow : Window
{
	private bool shouldReallyClose;

	public SelectZoneWindow(List<bool> options, Stream stream, Mutex streamMutex)
	{
		_ = streamMutex.WaitOne();
		InitializeComponent();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		for(int i = 0; i < options.Count; i++)
		{
			Button b = new()
			{
				Content = i
			};
			b.Click += (sender, _) =>
			{
				int zone = (int)((Button)sender!).Content!;
				stream.Write(new CToS_Packet(new CToS_Content.select_zone(new(zone: zone))).Serialize());
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
		Closed += (_, _) => streamMutex.ReleaseMutex();
	}
}
