using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils.Packets.Duel;
using Thrift.Protocol;
using Thrift.Transport.Client;

namespace CardGameClient;
public partial class YesNoWindow : Window
{
	readonly TcpClient client;
	private bool shouldReallyClose;
	public YesNoWindow(string description, TcpClient client)
	{
		InitializeComponent();
		MessageBlock.Text = description;
		this.client = client;
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		Closing += (sender, args) =>
		{
			args.Cancel = !shouldReallyClose;
		};
	}

	public async void YesClick(object? sender, RoutedEventArgs args)
	{
		await new ClientPacket.yes_no(new() { Yes = true }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
		shouldReallyClose = true;
		Close();
	}

	public async void NoClick(object? sender, RoutedEventArgs args)
	{
		await new ClientPacket.yes_no(new() { Yes = false }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
		shouldReallyClose = true;
		Close();
	}
}
