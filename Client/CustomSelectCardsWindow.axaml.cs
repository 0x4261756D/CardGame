using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CardGameUtils.CardConstants;
using Google.Protobuf;

namespace CardGameClient;

public partial class CustomSelectCardsWindow : Window
{
	private readonly Stream stream;
	private bool shouldReallyClose;
	private readonly Action<CardInfo> showCardAction;

	public CustomSelectCardsWindow(string text, IEnumerable<CardInfo> cards, bool initialState, Stream stream, int playerIndex, Action<CardInfo> showCardAction)
	{
		this.stream = stream;
		this.showCardAction = showCardAction;
		Monitor.Enter(stream);
		DataContext = new CustomSelectCardViewModel(text, initialState);
		InitializeComponent();
		Closed += (_, _) => Monitor.Exit(stream);
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

	public void ConfirmClick(object? sender, RoutedEventArgs args)
	{
		CardGameUtils.DuelClientToServer.CustomSelectCards payload = new();
		payload.Uids.AddRange(UIUtils.CardListBoxSelectionToUID(CardSelectionList));
		new CardGameUtils.DuelClientToServer.Packet { CustomSelectCards = payload }.WriteDelimitedTo(stream);
		shouldReallyClose = true;
		Close();
	}

	public void CardSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		CardGameUtils.DuelClientToServer.CustomSelectCardsIntermediate payload = new();
		payload.Uids.AddRange(UIUtils.CardListBoxSelectionToUID((ListBox)sender));
		new CardGameUtils.DuelClientToServer.Packet { CustomSelectCardsIntermediate = payload }.WriteDelimitedTo(stream);
		((CustomSelectCardViewModel)DataContext!).CanConfirm = CardGameUtils.DuelServerToClient.CustomSelectCardsIntermediate.Parser.ParseDelimitedFrom((NetworkStream)stream).IsValid;
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
