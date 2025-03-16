using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CardGameUtils.Base;
using CardGameUtils.Structs.Duel;
using System.Collections.Concurrent;

namespace CardGameClient;

internal partial class CustomSelectCardsWindow : Window
{
	private readonly Stream stream;
	private bool shouldReallyClose;
	private readonly Action<CardStruct> showCardAction;
	private readonly BlockingCollection<SToC_Content> packetContents;

	public CustomSelectCardsWindow(string text, List<CardStruct> cards, bool initialState, Stream stream, BlockingCollection<SToC_Content> packetContents, int playerIndex, Action<CardStruct> showCardAction)
	{
		this.stream = stream;
		this.showCardAction = showCardAction;
		this.packetContents = packetContents;
		InitializeComponent();
		MessageBox.Text = text;
		ConfirmButton.IsEnabled = initialState;
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
		ConfirmButton.IsEnabled = ((SToC_Content.select_cards_custom_intermediate)(packetContents.Take())).value.is_valid;
	}
}
