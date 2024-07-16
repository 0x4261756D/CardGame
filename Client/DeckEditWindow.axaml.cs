using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using CardGameUtils;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using CardGameUtils.Shared;
using CardGameUtils.Constants;
using Google.FlatBuffers;
using CardGameUtils.Packets.Deck;
using System.Net.Sockets;

namespace CardGameClient;

public partial class DeckEditWindow : Window
{
	private readonly Flyout moveFlyout = new();
	private List<CardInfoT> cardpool = [];

	public DeckEditWindow()
	{
		InitializeComponent();
		DataContext = new DeckEditWindowViewModel();
		if(DeckSelectBox.SelectedItem == null && DeckSelectBox.ItemCount > 0)
		{
			if(Program.config.last_deck_name != null)
			{
				foreach(object? item in DeckSelectBox.Items)
				{
					if((string?)item == Program.config.last_deck_name)
					{
						DeckSelectBox.SelectedItem = item;
						break;
					}
				}
				if(DeckSelectBox.SelectedItem == null)
				{
					DeckSelectBox.SelectedIndex = 0;
				}
			}
			else
			{
				DeckSelectBox.SelectedIndex = 0;
			}
		}
		DecklistPanel.LayoutUpdated += DecklistPanelInitialized;
	}

	private void DecklistPanelInitialized(object? sender, EventArgs e)
	{
		DecklistPanel.LayoutUpdated -= DecklistPanelInitialized;
		LoadSidebar("");
		LoadDeck(DeckSelectBox.SelectedItem!.ToString()!);
	}

	public void BackClick(object sender, RoutedEventArgs args)
	{
		new MainWindow
		{
			WindowState = this.WindowState,
		}.Show();
		this.Close();
	}
	public void SidebarGenericIncludeBoxClick(object? sender, RoutedEventArgs args)
	{
		LoadSidebar(SidebarTextBox?.Text ?? "");
	}
	public void LoadSidebar(string fil)
	{
		PlayerClass playerClass = (PlayerClass?)ClassSelectBox.SelectedItem ?? PlayerClass.All;
		using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		stream.Write(ClientPacketTToByteArray(new(){Content=new(){Type=ClientContent.search, Value = new ClientSearchPacketT{Filter=fil, IncludeGenericCards = SidebarGenericIncludeBox.IsChecked ?? false}}}));
		ServerPacket packet = Functions.ReadSizedDeckServerPacketFromStream(stream);
		if(packet.ContentType != ServerContent.search)
		{
			throw new Exception($"Expected packet of type `search` but got {packet.ContentType}");
		}
		cardpool = packet.ContentAssearch().UnPack().Cards;
		UIUtils.CacheArtworkBatchFromServer(cardpool.ConvertAll(x => x.Name));
		List<Control> items = [];
		foreach(CardInfoT c in cardpool)
		{
			Viewbox v = UIUtils.CreateGenericCard(c);
			v.PointerEntered += CardHover;
			items.Add(v);
		}
		SidebarList.ItemsSource = items;
	}

	private void CardHover(object? sender, PointerEventArgs args)
	{
		if(sender == null)
		{
			return;
		}
		if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			return;
		}
		CardInfoT c = (CardInfoT)((Control)sender).DataContext!;
		UIUtils.CardHover(CardImagePanel, CardTextBlock, c, true);
	}

	public void SidebarSelectionChanged(object? sender, SelectionChangedEventArgs args)
	{
		if(sender == null || cardpool == null || args.AddedItems.Count != 1 || args.RemovedItems.Count != 0)
		{
			return;
		}
		args.Handled = true;
		SidebarList.SelectedItem = null;
		Viewbox v = (Viewbox)args.AddedItems[0]!;
		CardInfoT card = (CardInfoT)v.DataContext!;
		if(card.TypeSpecifics.Type == TypeSpecifics.quest)
		{
			ClassQuestButton.Content = UIUtils.CreateGenericCard(card);
			ColorWrongThings((PlayerClass?)ClassSelectBox.SelectedItem);
		}
		else
		{
			if(DecklistPanel.Children.Count >= GameConstants.DECK_SIZE)
			{
				return;
			}
			int i = 0;
			foreach(Control c in DecklistPanel.Children)
			{
				if(((CardInfoT)((Viewbox)((Button)c).Content!).DataContext!).Name == card.Name)
				{
					i++;
					if(i >= GameConstants.MAX_CARD_MULTIPLICITY)
					{
						return;
					}
				}
			}
			DecklistPanel.Children.Add(CreateDeckButton(card));
		}
		SetDeckSize();
	}

	public void SetDeckSize()
	{
		DeckSizeBlock.Text = $"Deck size: {DecklistPanel.Children.Count}";
	}

	private void ContentRemoveClick(object sender, RoutedEventArgs args)
	{
		((Button)sender).Content = null;
		ColorWrongThings((PlayerClass?)ClassSelectBox.SelectedItem);
	}

	private void SortDeckClick(object sender, RoutedEventArgs args)
	{
		Control[] children = new Control[DecklistPanel.Children.Count];
		DecklistPanel.Children.CopyTo(children, 0);
		// This is fun, see no problem with this...
		Array.Sort(children, (child1, child2) => ((CardInfoT)((Control)((Button)child1).Content!).DataContext!).Name.CompareTo(((CardInfoT)((Control)((Button)child2).Content!).DataContext!).Name));
		DecklistPanel.Children.Clear();
		DecklistPanel.Children.AddRange(children);
	}

	public Button CreateDeckButton(CardInfoT c)
	{
		Button b = new()
		{
			DataContext = c,
			Padding = new Thickness(0, 0, 0, 0),
		};
		double xAmount = 10;
		double yAmount = Math.Ceiling(GameConstants.DECK_SIZE / xAmount);
		_ = DecklistBorder.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>((a) =>
		{
			b.Width = (a.Width - DecklistBorder.BorderThickness.Left - DecklistBorder.BorderThickness.Right - 20) / xAmount - (b.BorderThickness.Left + b.BorderThickness.Right);
			b.Height = (a.Height - DecklistBorder.BorderThickness.Top - DecklistBorder.BorderThickness.Bottom - 20) / yAmount - (b.BorderThickness.Top + b.BorderThickness.Bottom);
		}));
		Viewbox v = UIUtils.CreateGenericCard(c);
		b.Content = v;
		b.PointerPressed += RemoveCardClick;
		b.Click += MoveClick;
		b.PointerEntered += CardHover;
		return b;
	}
	private void RemoveCardClick(object? sender, RoutedEventArgs args)
	{
		if(sender == null)
		{
			return;
		}
		_ = DecklistPanel.Children.Remove((Button)sender);
		SetDeckSize();
	}
	private void MoveClick(object? sender, RoutedEventArgs e)
	{
		Button button = (Button)sender!;
		int index = DecklistPanel.Children.IndexOf(button);
		int max = DecklistPanel.Children.Count - 1;
		StackPanel panel = new();

		NumericUpDown numeric = new()
		{
			AllowSpin = true,
			Value = index,
			Minimum = 0,
			Maximum = max,
			Increment = 1,
		};
		panel.Children.Add(numeric);
		Button submitButton = new()
		{
			Content = new TextBlock
			{
				Text = "Move"
			}
		};
		submitButton.Click += (_, _) =>
		{
			int newInd = (int)numeric.Value;
			if(newInd < 0 || newInd > max)
			{
				return;
			}
			DecklistPanel.Children.RemoveAt(index);
			DecklistPanel.Children.Insert(newInd, button);
		};
		CardInfoT c = (CardInfoT)((Viewbox)button.Content!).DataContext!;
		if(c.TypeSpecifics.Type == TypeSpecifics.spell && c.TypeSpecifics.Asspell().CanBeClassAbility)
		{
			Button setAbilityButton = new()
			{
				Content = new TextBlock
				{
					Text = "Set as ability"
				}
			};
			setAbilityButton.Click += (_, _) =>
			{
				Viewbox v = UIUtils.CreateGenericCard(c);
				v.PointerEntered += CardHover;
				ClassAbilityButton.Content = v;
				ColorWrongThings((PlayerClass?)ClassSelectBox.SelectedItem);
			};
			panel.Children.Add(setAbilityButton);
		}
		panel.Children.Add(submitButton);
		moveFlyout.Content = panel;
		moveFlyout.ShowAt(button, true);
	}
	public void DeckSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		if(args.AddedItems.Count > 0 && DecklistPanel.Bounds.Width > 0 && DecklistPanel.Bounds.Height > 0)
		{
			Program.config.last_deck_name = args.AddedItems[0]?.ToString();
			LoadDeck(args?.AddedItems[0]!.ToString()!);
		}
	}
	public void LoadDeck(string deckName)
	{
		DecklistPanel.Children.Clear();
		using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		stream.Write(ClientPacketTToByteArray(new(){Content=new(){Type = ClientContent.list, Value = new ClientListPacketT(){Name = deckName}}}));
		ServerPacket packet = Functions.ReadSizedDeckServerPacketFromStream(stream);
		if(packet.ContentType != ServerContent.list)
		{
			throw new Exception($"Expected packet of type `list` but got {packet.ContentType}");
		}
		DeckInfoT? deck = packet.ContentAslist().Deck?.UnPack();
		if(deck is null)
		{
			return;
		}
		if(deck.PlayerClass == PlayerClass.UNKNOWN)
		{
			ClassSelectBox.SelectedIndex = -1;
		}
		else
		{
			ClassSelectBox.SelectedItem = deck.PlayerClass;
		}
		UIUtils.CacheArtworkBatchFromServer(deck.Cards.ConvertAll(x => x.Name));
		foreach(CardInfoT c in deck.Cards)
		{
			DecklistPanel.Children.Add(CreateDeckButton(c));
		}
		ClassAbilityButton.Content = null;
		if(deck.Ability is not null)
		{
			Viewbox v = UIUtils.CreateGenericCard(deck.Ability);
			v.PointerEntered += CardHover;
			ClassAbilityButton.Content = v;
		}
		ClassQuestButton.Content = null;
		if(deck.Quest is not null)
		{
			Viewbox v = UIUtils.CreateGenericCard(deck.Quest);
			v.PointerEntered += CardHover;
			ClassQuestButton.Content = v;
		}
		SetDeckSize();
		ColorWrongThings(deck.PlayerClass);
	}
	public void ClassSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		PlayerClass? playerClass = args.AddedItems.Count > 0 ? (PlayerClass?)args.AddedItems?[0] : null;
		LoadSidebar(SidebarTextBox?.Text ?? "");
		ColorWrongThings(playerClass);
	}

	private void ColorWrongThings(PlayerClass? playerClass)
	{
		foreach(Control c in DecklistPanel.Children)
		{
			Button child = (Button)c;
			// Oh boy, do I love GUI programming...
			PlayerClass cardClass = ((CardInfoT)((Viewbox)child.Content!).DataContext!).CardClass;
			if(cardClass != PlayerClass.All && playerClass != PlayerClass.All &&
				cardClass != playerClass)
			{
				child.BorderBrush = Brushes.Red;
				child.BorderThickness = new Thickness(5);
			}
			else
			{
				child.BorderBrush = null;
				child.BorderThickness = new Thickness(1);
			}
		}
		if(ClassQuestButton.Content != null)
		{
			PlayerClass cardClass = ((CardInfoT)((Viewbox)ClassQuestButton.Content).DataContext!).CardClass;
			if(cardClass != PlayerClass.All && playerClass != PlayerClass.All &&
				cardClass != playerClass)
			{
				ClassQuestButton.BorderBrush = Brushes.Red;
				ClassQuestButton.BorderThickness = new Thickness(5);
			}
			else
			{
				ClassQuestButton.BorderBrush = null;
				ClassQuestButton.BorderThickness = new Thickness(1);
			}
		}
		else
		{
			ClassQuestButton.BorderBrush = null;
			ClassQuestButton.BorderThickness = new Thickness(1);
		}
		if(ClassAbilityButton.Content != null)
		{
			PlayerClass cardClass = ((CardInfoT)((Viewbox)ClassAbilityButton.Content).DataContext!).CardClass;
			if(cardClass != PlayerClass.All && playerClass != PlayerClass.All &&
				cardClass != playerClass)
			{
				ClassAbilityButton.BorderBrush = Brushes.Red;
				ClassAbilityButton.BorderThickness = new Thickness(5);
			}
			else
			{
				ClassAbilityButton.BorderBrush = null;
				ClassAbilityButton.BorderThickness = new Thickness(1);
			}
		}
		else
		{
			ClassAbilityButton.BorderBrush = null;
			ClassAbilityButton.BorderThickness = new Thickness(1);
		}
	}

	public void CreateNewDeckClick(object? sender, RoutedEventArgs args)
	{
		string? newName = NewDeckName.Text;
		if(newName is null or "")
		{
			return;
		}
		using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		stream.Write(ClientPacketTToByteArray(new(){Content=new(){Type = ClientContent.update, Value = new ClientUpdatePacketT(){Deck = new(){Name = newName}}}}));
		((DeckEditWindowViewModel)DataContext!).Decknames.Add(newName);
		DeckSelectBox.SelectedItem = newName;
		DeckSizeBlock.Text = "Deck size: 0";
		NewDeckName.Text = "";
	}
	public void SaveDeckClick(object? sender, RoutedEventArgs args)
	{
		if(DeckSelectBox.SelectedItem == null || string.IsNullOrEmpty((string)DeckSelectBox.SelectedItem))
		{
			return;
		}
		PlayerClass playerClass = (PlayerClass?)ClassSelectBox.SelectedItem ?? PlayerClass.UNKNOWN;
		if(playerClass == PlayerClass.All)
		{
			playerClass = PlayerClass.UNKNOWN;
		}
		Viewbox? abilityBox = (Viewbox?)ClassAbilityButton.Content;
		CardInfoT? ability = abilityBox == null ? null : (CardInfoT?)abilityBox.DataContext;
		Viewbox? questBox = (Viewbox?)ClassQuestButton.Content;
		CardInfoT? quest = questBox == null ? null : (CardInfoT?)questBox.DataContext;
		Control[] children = new Control[DecklistPanel.Children.Count];
		DecklistPanel.Children.CopyTo(children, 0);
		using TcpClient client = new TcpClient(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		stream.Write(ClientPacketTToByteArray(new()
		{
			Content = new()
			{
				Type = ClientContent.update,
				Value = new ClientUpdatePacketT
				{
					Deck = new()
					{
						Cards = [.. Array.ConvertAll(children, child => (CardInfoT)((Viewbox)((Button)child).Content!).DataContext!)],
						Ability = ability,
						Quest = quest,
						PlayerClass = playerClass,
						Name = (string)DeckSelectBox.SelectedItem!
					}
				}
			}
		}));
	}
	public void DeleteDeckClick(object? sender, RoutedEventArgs args)
	{
		using TcpClient client = new TcpClient(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		stream.Write(ClientPacketTToByteArray(new()
		{
			Content = new()
			{
				Type = ClientContent.delete,
				Value = new ClientDeletePacketT
				{
					Name = (string)DeckSelectBox.SelectedItem!
				}
			}
		}));
		_ = ((DeckEditWindowViewModel)DataContext!).Decknames.Remove((string)DeckSelectBox.SelectedItem!);
		DeckSelectBox.SelectedIndex = DeckSelectBox.ItemCount - 1;
	}
	public void SidebarTextInput(object? sender, KeyEventArgs args)
	{
		TextBox? tb = (TextBox?)sender;
		LoadSidebar(tb?.Text ?? "");
	}
	public static byte[] ClientPacketTToByteArray(ClientPacketT packet)
	{
		FlatBufferBuilder builder = new(1);
		builder.FinishSizePrefixed(ClientPacket.Pack(builder, packet).Value);
		return builder.DataBuffer.ToSizedArray();
	}

}


public class DeckEditWindowViewModel : INotifyPropertyChanged
{
	public DeckEditWindowViewModel()
	{
		LoadDecks();
	}

	public void LoadDecks()
	{
		using TcpClient client = new TcpClient(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		stream.Write(DeckEditWindow.ClientPacketTToByteArray(new()
		{
			Content = new()
			{
				Type = ClientContent.names,
				Value = new ClientNamesPacketT()
			}
		}));
		ServerPacket packet = Functions.ReadSizedDeckServerPacketFromStream(stream);
		if(packet.ContentType != ServerContent.names)
		{
			throw new Exception($"Expected packet of type `names` but got {packet.ContentType}");
		}
		List<string> names = packet.ContentAsnames().UnPack().Names;
		names.Sort();
		Decknames.Clear();
		foreach(string name in names)
		{
			Decknames.Add(name);
		}
		_ = classes.Remove(PlayerClass.UNKNOWN);
	}

	public event PropertyChangedEventHandler? PropertyChanged;


	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private readonly ObservableCollection<PlayerClass> classes = new(Enum.GetValues<PlayerClass>());
	public ObservableCollection<PlayerClass> Classes
	{
		get => classes;
	}
	private ObservableCollection<string> decknames = [];
	public ObservableCollection<string> Decknames
	{
		get => decknames;
		set
		{
			if(value != decknames)
			{
				decknames = value;
				NotifyPropertyChanged();
			}
		}
	}
}
