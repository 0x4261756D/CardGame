using System;
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
using CardGameUtils.Structs;
using CardGameUtils.Shared;
using CardGameUtils.Packets.Duel;
using System.Collections.Generic;
using CardGameUtils;

namespace CardGameClient;

public partial class CustomSelectCardsWindow : Window
{
	private readonly Stream stream;
	private bool shouldReallyClose;
	private readonly Action<CardInfoT> showCardAction;

	public CustomSelectCardsWindow(ServerCustomSelectCardsPacket request, Stream stream, int playerIndex, Action<CardInfoT> showCardAction)
	{
		this.stream = stream;
		this.showCardAction = showCardAction;
		Monitor.Enter(stream);
		DataContext = new CustomSelectCardViewModel(request.Description, request.InitialState);
		InitializeComponent();
		Closed += (_, _) => Monitor.Exit(stream);
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		CardSelectionList.MaxHeight = Program.config.height / 3;
		List<CardInfoT> cards = request.UnPack().Cards;
		CardSelectionList.DataContext = cards;
		CardSelectionList.ItemsSource = cards;
		CardSelectionList.ItemTemplate = new FuncDataTemplate<CardInfoT>((value, namescope) =>
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
		showCardAction((CardInfoT)((Control)sender).DataContext!);
	}

	public void ConfirmClick(object? sender, RoutedEventArgs args)
	{
		stream.Write(DuelWindow.ClientPacketTToByteArray(new()
		{
			Content = new()
			{
				Type = ClientContent.customselectcards,
				Value = new ClientCustomSelectCardsPacketT
				{
					Uids = UIUtils.CardListBoxSelectionToUID(CardSelectionList),
				}
			}
		}));
		shouldReallyClose = true;
		Close();
	}

	public void CardSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		stream.Write(DuelWindow.ClientPacketTToByteArray(new()
		{
			Content = new()
			{
				Type = ClientContent.customselectintermediate,
				Value = new ClientCustomSelectIntermediatePacketT
				{
					Uids = UIUtils.CardListBoxSelectionToUID((ListBox)sender!),
				}
			}
		}));
		ServerPacket packet = Functions.ReadSizedDuelServerPacketFromStream(stream);
		if(packet.ContentType != ServerContent.customselectintermediate)
		{
			throw new Exception($"Expected packet of type `custom select intermediate` but got {packet.ContentType}");
		}
		((CustomSelectCardViewModel)DataContext!).CanConfirm = packet.ContentAscustomselectintermediate().IsValid;
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
