using System.Collections.Generic;
using System.Net.Sockets;
using Avalonia.Controls;
using CardGameUtils.Packets.Duel;
using Thrift.Protocol;
using Thrift.Transport.Client;

namespace CardGameClient;

public partial class SelectZoneWindow : Window
{
	private bool shouldReallyClose;

	public SelectZoneWindow(List<bool> options, TcpClient client)
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
			b.Click += async (sender, _) =>
			{
				int zone = (int)((Button)sender!).Content!;
				await new ClientPacket.select_zone(new() { Zone = zone }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
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
