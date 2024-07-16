using System.IO;
using Avalonia.Controls;
using CardGameUtils.Packets.Duel;
using System.Collections.Generic;

namespace CardGameClient;

public partial class SelectZoneWindow : Window
{
	private bool shouldReallyClose;

	public SelectZoneWindow(List<bool> options, Stream stream)
	{
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
				stream.Write(DuelWindow.ClientPacketTToByteArray(new(){Content=new(){Type=ClientContent.selectzone, Value = new ClientSelectZonePacketT()
				{
					Zone = zone
				}}}));
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
