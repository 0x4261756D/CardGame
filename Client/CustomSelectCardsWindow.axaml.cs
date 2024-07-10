using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Duel;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;

namespace CardGameClient;

public partial class CustomSelectCardsWindow : Window
{
	private readonly TcpClient client;
	private bool shouldReallyClose;
	private readonly Action<CardInfo> showCardAction;

	public CustomSelectCardsWindow(string text, List<CardInfo> cards, bool initialState, TcpClient client, int playerIndex, Action<CardInfo> showCardAction)
	{
		this.client = client;
		this.showCardAction = showCardAction;
		Monitor.Enter(client);
		DataContext = new CustomSelectCardViewModel(text, initialState);
		InitializeComponent();
		Closed += (_, _) => Monitor.Exit(client);
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		CardSelectionList.MaxHeight = Program.config.height / 3;
		CardSelectionList.DataContext = cards;
		CardSelectionList.ItemsSource = cards;
		CardSelectionList.ItemTemplate = new FuncDataTemplate<CardInfo>((value, namescope) =>
		{
			TextBlock block = new()
			{
				Text = value.Name,
				TextAlignment = (value.Controller == playerIndex) ? TextAlignment.Left : TextAlignment.Right,
			};
			Border border = new()
			{
				Child = block,
				Background = Brushes.Transparent,
			};
			border.PointerEntered += CardPointerEntered;
			return border;
		});
		Closing += (_, args) =>
		{
			args.Cancel = !shouldReallyClose;
		};
	}
	private void CardPointerEntered(object? sender, PointerEventArgs args)
	{
		if(sender == null)
		{
			return;
		}
		if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			return;
		}
		showCardAction((CardInfo)((Control)sender).DataContext!);
	}

	public async void ConfirmClick(object? sender, RoutedEventArgs args)
	{
		await new ClientPacket.custom_select_cards(new() { Uids = UIUtils.CardListBoxSelectionToUID(CardSelectionList) }).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
		shouldReallyClose = true;
		Close();
	}

	public async void CardSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		TTransport transport = new TSocketTransport(client, new());
		await new ClientPacket.custom_select_cards_intermediate(new() { Uids = UIUtils.CardListBoxSelectionToUID((ListBox)sender) }).WriteAsync(new TCompactProtocol(transport), default);
		ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		((CustomSelectCardViewModel)DataContext!).CanConfirm = packet.As_custom_select_cards_intermediate!.Is_valid;
	}
}
public class CustomSelectCardViewModel : INotifyPropertyChanged
{
	public CustomSelectCardViewModel(string message, bool initialState)
	{
		Message = message;
		NotifyPropertyChanged("Message");
		CanConfirm = initialState;
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public readonly string Message;
	private bool canConfirm;
	public bool CanConfirm
	{
		get => canConfirm;
		set
		{
			if(value != canConfirm)
			{
				canConfirm = value;
				NotifyPropertyChanged();
			}
		}
	}
}
