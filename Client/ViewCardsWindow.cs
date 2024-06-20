using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using CardGameUtils.CardConstants;

namespace CardGameClient;

public partial class ViewCardsWindow : Window
{
	private readonly Action<CardInfo> showCardAction;

	public ViewCardsWindow(IEnumerable<CardInfo> cards, string? message, Action<CardInfo> showCardAction)
	{
		InitializeComponent();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		this.showCardAction = showCardAction;
		CardSelectionList.MaxHeight = Program.config.height / 3;
		CardSelectionList.DataContext = cards;
		CardSelectionList.ItemsSource = cards;
		CardSelectionList.ItemTemplate = new FuncDataTemplate<CardInfo>((value, _) =>
		{
			TextBlock block = new()
			{
				Text = value.Name,
			};
			Border border = new()
			{
				Child = block,
				Background = Avalonia.Media.Brushes.Transparent,
			};
			border.PointerEntered += CardPointerEntered;
			return border;
		});
		if(message != null)
		{
			Message.Text = message;
		}

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
	public void CloseClick(object? sender, RoutedEventArgs args)
	{
		Close();
	}
}
