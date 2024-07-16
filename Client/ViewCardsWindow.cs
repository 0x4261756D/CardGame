using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using CardGameUtils.Structs;
using CardGameUtils.Shared;
using System.Collections.Generic;

namespace CardGameClient;

public partial class ViewCardsWindow : Window
{
	private readonly Action<CardInfoT> showCardAction;

	public ViewCardsWindow(List<CardInfoT> cards, string? message, Action<CardInfoT> showCardAction)
	{
		InitializeComponent();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		this.showCardAction = showCardAction;
		CardSelectionList.MaxHeight = Program.config.height / 3;
		CardSelectionList.DataContext = cards;
		CardSelectionList.ItemsSource = cards;
		CardSelectionList.ItemTemplate = new FuncDataTemplate<CardInfoT>((value, _) =>
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
		showCardAction((CardInfoT)((Control)sender).DataContext!);
	}
	public void CloseClick(object? sender, RoutedEventArgs args)
	{
		Close();
	}
}
