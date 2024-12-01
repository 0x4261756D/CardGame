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
using CardGameUtils.Base;
using CardGameUtils.Structs.Duel;

namespace CardGameClient;

public partial class CustomSelectCardsWindow : Window
{
	private readonly Stream stream;
	private bool shouldReallyClose;
	private readonly Action<CardStruct> showCardAction;

	public CustomSelectCardsWindow(string text, List<CardStruct> cards, bool initialState, Stream stream, Mutex streamMutex, int playerIndex, Action<CardStruct> showCardAction)
	{
		this.stream = stream;
		_ = streamMutex.WaitOne();
		this.showCardAction = showCardAction;
		DataContext = new CustomSelectCardViewModel(text, initialState);
		InitializeComponent();
		Closed += (_, _) => streamMutex.ReleaseMutex();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		CardSelectionList.MaxHeight = Program.config.height / 3;
		CardSelectionList.DataContext = cards;
		CardSelectionList.ItemsSource = cards;
		CardSelectionList.ItemTemplate = new FuncDataTemplate<CardStruct>((value, namescope) =>
		{
			TextBlock block = new()
			{
				Text = value.name,
				TextAlignment = (value.controller == playerIndex) ? TextAlignment.Left : TextAlignment.Right,
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
		showCardAction((CardStruct)((Control)sender).DataContext!);
	}

	public void ConfirmClick(object? sender, RoutedEventArgs args)
	{
		stream.Write(new CToS_Packet(new CToS_Content.select_cards_custom(new(uids: UIUtils.CardListBoxSelectionToUID(CardSelectionList)))).Serialize());
		shouldReallyClose = true;
		Close();
	}

	public void CardSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		stream.Write(new CToS_Packet(new CToS_Content.select_cards_custom_intermediate(new(uids: UIUtils.CardListBoxSelectionToUID((ListBox)sender)))).Serialize());
		((CustomSelectCardViewModel)DataContext!).CanConfirm = ReceivePacket<SToC_Content.select_cards_custom_intermediate>((NetworkStream)stream)!.value.is_valid;
	}

	public static T ReceivePacket<T>(NetworkStream stream) where T : SToC_Content
	{
		return (T)SToC_Packet.Deserialize(stream).content;
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
