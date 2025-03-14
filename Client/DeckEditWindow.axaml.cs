using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.GameEnumsAndStructs;
using CardGameUtils.Structs.Deck;

namespace CardGameClient;

internal partial class DeckEditWindow : Window
{
	private readonly Flyout moveFlyout = new();
	private List<CardStruct> cardpool = [];

	public DeckEditWindow()
	{
		InitializeComponent();
		LoadDecks();
		foreach(var foo in Enum.GetValues<PlayerClass>())
		{
			if(foo != PlayerClass.UNKNOWN)
			{
				ClassSelectBox.Items.Add(foo);
			}
		}
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
		cardpool = SendAndReceive<SToC_Content.search>(new CToS_Content.search(new(filter: fil, player_class: playerClass, include_generic_cards: SidebarGenericIncludeBox.IsChecked ?? false)),
			Program.config.deck_edit_url.address, Program.config.deck_edit_url.port).value.cards;
		UIUtils.CacheArtworkBatchFromServer(cardpool.ConvertAll(x => x.name));
		List<Control> items = [];
		foreach(CardStruct c in cardpool)
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
		CardStruct c = (CardStruct)((Control)sender).DataContext!;
		UIUtils.CardHover(CardImagePanel, CardTextBlock, c, false);
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
		CardStruct card = (CardStruct)v.DataContext!;
		if(card.type_specifics is TypeSpecifics.quest)
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
				if(((CardStruct)((Viewbox)((Button)c).Content!).DataContext!).name == card.name)
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
		Array.Sort(children, (child1, child2) => ((CardStruct)((Control)((Button)child1).Content!).DataContext!).name.CompareTo(((CardStruct)((Control)((Button)child2).Content!).DataContext!).name));
		DecklistPanel.Children.Clear();
		DecklistPanel.Children.AddRange(children);
	}

	public Button CreateDeckButton(CardStruct c)
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
		CardStruct c = (CardStruct)((Viewbox)button.Content!).DataContext!;
		if(c.type_specifics is TypeSpecifics.spell spell && spell.value.can_be_class_ability)
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
		Deck? deck = SendAndReceive<SToC_Content.decklist>(new CToS_Content.decklist(new(name: deckName)), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port).value.deck;
		if(deck is null)
		{
			new ErrorPopup($"No deck named {deckName} found").Show();
			return;
		}
		if(deck.player_class == PlayerClass.UNKNOWN)
		{
			ClassSelectBox.SelectedIndex = -1;
		}
		else
		{
			ClassSelectBox.SelectedItem = deck.player_class;
		}
		UIUtils.CacheArtworkBatchFromServer(deck.cards.ConvertAll(x => x.name));
		foreach(CardStruct c in deck.cards)
		{
			DecklistPanel.Children.Add(CreateDeckButton(c));
		}
		ClassAbilityButton.Content = null;
		if(deck.ability != null)
		{
			Viewbox v = UIUtils.CreateGenericCard(deck.ability);
			v.PointerEntered += CardHover;
			ClassAbilityButton.Content = v;
		}
		ClassQuestButton.Content = null;
		if(deck.quest != null)
		{
			Viewbox v = UIUtils.CreateGenericCard(deck.quest);
			v.PointerEntered += CardHover;
			ClassQuestButton.Content = v;
		}
		SetDeckSize();
		ColorWrongThings(deck.player_class);
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
			PlayerClass cardClass = ((CardStruct)((Viewbox)child.Content!).DataContext!).card_class;
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
			PlayerClass cardClass = ((CardStruct)((Viewbox)ClassQuestButton.Content).DataContext!).card_class;
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
			PlayerClass cardClass = ((CardStruct)((Viewbox)ClassAbilityButton.Content).DataContext!).card_class;
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
		Send(new CToS_Content.decklist_update(new
		(
			deck: new
			(
				cards: [],
				name: newName,
				player_class: PlayerClass.UNKNOWN,
				ability: null,
				quest: null
			)
		)), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		DeckSelectBox.Items.Add(newName);
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
		CardStruct? ability = abilityBox == null ? null : (CardStruct?)abilityBox.DataContext;
		Viewbox? questBox = (Viewbox?)ClassQuestButton.Content;
		CardStruct? quest = questBox == null ? null : (CardStruct?)questBox.DataContext;
		Control[] children = new Control[DecklistPanel.Children.Count];
		DecklistPanel.Children.CopyTo(children, 0);
		Send(new CToS_Content.decklist_update(new
		(
			deck: new
			(
				cards: [.. Array.ConvertAll(children, child => (CardStruct)((Viewbox)((Button)child).Content!).DataContext!)],
				ability: ability,
				quest: quest,
				player_class: playerClass,
				name: (string)DeckSelectBox.SelectedItem!
			)
		)), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
	}
	public void DeleteDeckClick(object? sender, RoutedEventArgs args)
	{
		Send(new CToS_Content.decklist_delete(new((string)DeckSelectBox.SelectedItem!)), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		DeckSelectBox.Items.Remove((string)DeckSelectBox.SelectedItem!);
		DeckSelectBox.SelectedIndex = DeckSelectBox.ItemCount - 1;
	}
	public void SidebarTextInput(object? sender, KeyEventArgs args)
	{
		TextBox? tb = (TextBox?)sender;
		LoadSidebar(tb?.Text ?? "");
	}

	public static T? TrySendAndReceive<T>(CToS_Content content, string address, int port) where T : SToC_Content
	{
		try
		{
			return SendAndReceive<T>(content, address, port);
		}
		catch(Exception ex)
		{
			new ErrorPopup(ex.Message).Show();
			return default;
		}
	}
	public static T SendAndReceive<T>(CToS_Content content, string address, int port) where T : SToC_Content
	{
		using TcpClient client = new(address, port);
		using NetworkStream stream = client.GetStream();
		stream.Write(new CToS_Packet(content).Serialize());
		return (T)SToC_Packet.Deserialize(stream).content;
	}
	private static void Send(CToS_Content content, string address, int port)
	{
		using TcpClient client = new(address, port);
		using NetworkStream stream = client.GetStream();
		stream.Write(new CToS_Packet(content).Serialize());
	}
	public void LoadDecks()
	{
		List<string> names = DeckEditWindow.SendAndReceive<SToC_Content.decklists>(new CToS_Content.decklists(), Program.config.deck_edit_url.address, Program.config.deck_edit_url.port).value.names;
		names.Sort();
		DeckSelectBox.Items.Clear();
		foreach(string name in names)
		{
			DeckSelectBox.Items.Add(name);
		}
	}
}

