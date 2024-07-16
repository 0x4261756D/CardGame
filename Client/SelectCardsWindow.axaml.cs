using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using CardGameUtils.Shared;
using CardGameUtils.Packets.Duel;
using System.Collections.Generic;

namespace CardGameClient;

public partial class SelectCardsWindow : Window
{
	private readonly Stream stream;
	private bool shouldReallyClose;
	private readonly Action<CardInfoT> showCardAction;

	public SelectCardsWindow(ServerSelectCardsPacket packet, Stream stream, int playerIndex, Action<CardInfoT> showCardAction)
	{
		if(packet.CardsLength < packet.Amount)
		{
			throw new Exception($"Tried to create a SelectCardWindow requiring to select more cards than possible: {packet.CardsLength}/{packet.Amount}");
		}
		this.showCardAction = showCardAction;
		this.stream = stream;
		DataContext = new SelectedCardViewModel(packet.Amount);
		InitializeComponent();
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		CardSelectionList.MaxHeight = Program.config.height / 3;
		List<CardInfoT> cards = packet.UnPack().Cards;
		CardSelectionList.DataContext = cards;
		CardSelectionList.ItemsSource = cards;
		CardSelectionList.ItemTemplate = new FuncDataTemplate<CardInfoT>((value, namescope) =>
		{
			TextBlock block = new()
			{
				Text = value.Name,
				TextAlignment = (playerIndex == value.Controller) ? TextAlignment.Left : TextAlignment.Right,
			};
			Border border = new()
			{
				Child = block,
				Background = Brushes.Transparent,
			};
			border.PointerEntered += CardPointerEntered;
			return border;
		});
		Message.Text = packet.Description;
		Amount.Text = $"/ {packet.Amount}";
		Closing += (sender, args) =>
		{
			args.Cancel = !shouldReallyClose;
		};
		if(packet.Amount == 1)
		{
			CardSelectionList.SelectionMode = SelectionMode.Single | SelectionMode.Toggle;
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

	public void CardSelectionChanged(object? sender, SelectionChangedEventArgs args)
	{
		if(sender == null)
		{
			return;
		}
		int newCount = ((ListBox)sender).SelectedItems?.Count ?? 0;
		((SelectedCardViewModel)DataContext!).SelectedCount = newCount;
	}

	public void ConfirmClick(object? sender, RoutedEventArgs args)
	{
		stream.Write(DuelWindow.ClientPacketTToByteArray(new()
		{
			Content = new()
			{
				Type = ClientContent.selectcards,
				Value = new ClientSelectCardsPacketT
				{
					Uids = UIUtils.CardListBoxSelectionToUID(CardSelectionList),
				},
			}
		}));
		shouldReallyClose = true;
		Close();
	}
}

public class SelectedCardViewModel : INotifyPropertyChanged
{
	public SelectedCardViewModel(int amount)
	{
		Amount = amount;
		NotifyPropertyChanged("Amount");
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public readonly int Amount;

	public bool CanConfirm
	{
		get => SelectedCount == Amount;
	}

	private int selectedCount;
	public int SelectedCount
	{
		get => selectedCount;
		set
		{
			if(selectedCount != value)
			{
				selectedCount = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(CanConfirm));
			}
		}
	}
}
