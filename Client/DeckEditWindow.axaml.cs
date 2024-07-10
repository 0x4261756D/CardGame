using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using CardGameUtils;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Deck;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;

namespace CardGameClient;

public partial class DeckEditWindow : Window
{
	private readonly Flyout moveFlyout = new();
	private List<CardInfo> cardpool = [];

	public DeckEditWindow()
	{
		InitializeComponent();
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

	private async void DecklistPanelInitialized(object? sender, EventArgs e)
	{
		DecklistPanel.LayoutUpdated -= DecklistPanelInitialized;
		await LoadSidebar("");
		await LoadDecks();
		ClassSelectBox.ItemsSource = Enum.GetValues<PlayerClass>();
		DeckSelectBox.SelectedIndex = 0;
		await LoadDeck(DeckSelectBox.SelectedItem!.ToString()!);
	}

	public void BackClick(object sender, RoutedEventArgs args)
	{
		new MainWindow
		{
			WindowState = this.WindowState,
		}.Show();
		this.Close();
	}
	public async void SidebarGenericIncludeBoxClick(object? sender, RoutedEventArgs args)
	{
		await LoadSidebar(SidebarTextBox?.Text ?? "");
	}
	public async Task LoadSidebar(string fil)
	{
		PlayerClass playerClass = (PlayerClass?)ClassSelectBox.SelectedItem ?? PlayerClass.All;
		Functions.Log("Now sending search request");
		Functions.Log($"{Program.config.deck_edit_url.address}:{Program.config.deck_edit_url.port}");
		TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, new());
		await new ClientPacket.search(new() { Filter = fil, Include_generic_cards = SidebarGenericIncludeBox.IsChecked ?? false, Player_class = playerClass }).WriteAsync(new TCompactProtocol(transport), default);
		Functions.Log("Sent");
		ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		Functions.Log("got search request");
		cardpool = packet.As_search!.Cards ?? [];
		await UIUtils.CacheArtworkBatchFromServer(cardpool.ConvertAll(x => x.Name!));
		List<Control> items = [];
		foreach(CardInfo c in cardpool)
		{
			Viewbox v = await UIUtils.CreateGenericCard(c);
			v.PointerEntered += CardHover;
			items.Add(v);
		}
		SidebarList.ItemsSource = items;
	}

	private async void CardHover(object? sender, PointerEventArgs args)
	{
		if(sender == null)
		{
			return;
		}
		if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			return;
		}
		CardInfo c = (CardInfo)((Control)sender).DataContext!;
		await UIUtils.CardHover(CardImagePanel, CardTextBlock, c, true);
	}

	public async void SidebarSelectionChanged(object? sender, SelectionChangedEventArgs args)
	{
		if(sender == null || cardpool == null || args.AddedItems.Count != 1 || args.RemovedItems.Count != 0)
		{
			return;
		}
		args.Handled = true;
		SidebarList.SelectedItem = null;
		Viewbox v = (Viewbox)args.AddedItems[0]!;
		CardInfo card = (CardInfo)v.DataContext!;
		if(card.Card_type_specifics is CardTypeSpecifics.quest)
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
				if(((CardInfo)((Viewbox)((Button)c).Content!).DataContext!).Name == card.Name)
				{
					i++;
					if(i >= GameConstants.MAX_CARD_MULTIPLICITY)
					{
						return;
					}
				}
			}
			DecklistPanel.Children.Add(await CreateDeckButton(card));
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
		Array.Sort(children, (child1, child2) => ((CardInfo)((Control)((Button)child1).Content!).DataContext!).Name!.CompareTo(((CardInfo)((Control)((Button)child2).Content!).DataContext!).Name!));
		DecklistPanel.Children.Clear();
		DecklistPanel.Children.AddRange(children);
	}

	public async Task<Button> CreateDeckButton(CardInfo c)
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
		Viewbox v = await UIUtils.CreateGenericCard(c);
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
		CardInfo c = (CardInfo)((Viewbox)button.Content!).DataContext!;
		if(c.Card_type_specifics is CardTypeSpecifics.spell && c.Card_type_specifics.As_spell!.Can_be_class_ability)
		{
			Button setAbilityButton = new()
			{
				Content = new TextBlock
				{
					Text = "Set as ability"
				}
			};
			setAbilityButton.Click += async (_, _) =>
			{
				Viewbox v = await UIUtils.CreateGenericCard(c);
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
	public async void DeckSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		if(args.AddedItems.Count > 0 && DecklistPanel.Bounds.Width > 0 && DecklistPanel.Bounds.Height > 0)
		{
			Program.config.last_deck_name = args.AddedItems[0]?.ToString();
			await LoadDeck(args?.AddedItems[0]!.ToString()!);
		}
	}
	public async Task LoadDeck(string deckName)
	{
		DecklistPanel.Children.Clear();
		TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, new());
		await new ClientPacket.list(new() { Name = deckName }).WriteAsync(new TCompactProtocol(transport), default);
		ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		DeckInfo? deck = packet.As_list!.Deck;
		if(deck is null)
		{
			return;
		}
		if(deck.Player_class == PlayerClass.All)
		{
			ClassSelectBox.SelectedIndex = -1;
		}
		else
		{
			ClassSelectBox.SelectedItem = deck.Player_class;
		}
		if(deck.Cards is not null)
		{
			await UIUtils.CacheArtworkBatchFromServer(deck.Cards.ConvertAll(x => x.Name!));
			foreach(CardInfo c in deck.Cards)
			{
				DecklistPanel.Children.Add(await CreateDeckButton(c));
			}
		}
		ClassAbilityButton.Content = null;
		if(deck.Ability is not null)
		{
			Viewbox v = await UIUtils.CreateGenericCard(deck.Ability);
			v.PointerEntered += CardHover;
			ClassAbilityButton.Content = v;
		}
		ClassQuestButton.Content = null;
		if(deck.Quest is not null)
		{
			Viewbox v = await UIUtils.CreateGenericCard(deck.Quest);
			v.PointerEntered += CardHover;
			ClassQuestButton.Content = v;
		}
		SetDeckSize();
		ColorWrongThings(deck.Player_class);
	}
	public async void ClassSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		PlayerClass? playerClass = args.AddedItems.Count > 0 ? (PlayerClass?)args.AddedItems?[0] : null;
		await LoadSidebar(SidebarTextBox?.Text ?? "");
		ColorWrongThings(playerClass);
	}

	private void ColorWrongThings(PlayerClass? playerClass)
	{
		foreach(Control c in DecklistPanel.Children)
		{
			Button child = (Button)c;
			// Oh boy, do I love GUI programming...
			PlayerClass cardClass = ((CardInfo)((Viewbox)child.Content!).DataContext!).Card_class;
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
			PlayerClass cardClass = ((CardInfo)((Viewbox)ClassQuestButton.Content).DataContext!).Card_class;
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
			PlayerClass cardClass = ((CardInfo)((Viewbox)ClassAbilityButton.Content).DataContext!).Card_class;
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

	public async void CreateNewDeckClick(object? sender, RoutedEventArgs args)
	{
		string? newName = NewDeckName.Text;
		if(newName is null or "")
		{
			return;
		}
		TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, new());
		await new ClientPacket.update_list(new() { Deck = new() { Name = newName } }).WriteAsync(new TCompactProtocol(transport), default);
		_ = DeckSelectBox.Items.Add(newName);
		DeckSelectBox.SelectedItem = newName;
		DeckSizeBlock.Text = "Deck size: 0";
		NewDeckName.Text = "";
	}
	public async void SaveDeckClick(object? sender, RoutedEventArgs args)
	{
		if(DeckSelectBox.SelectedItem == null || string.IsNullOrEmpty((string)DeckSelectBox.SelectedItem))
		{
			return;
		}
		PlayerClass playerClass = (PlayerClass?)ClassSelectBox.SelectedItem ?? PlayerClass.All;
		Viewbox? abilityBox = (Viewbox?)ClassAbilityButton.Content;
		CardInfo? ability = abilityBox == null ? null : (CardInfo?)abilityBox.DataContext;
		Viewbox? questBox = (Viewbox?)ClassQuestButton.Content;
		CardInfo? quest = questBox == null ? null : (CardInfo?)questBox.DataContext;
		Control[] children = new Control[DecklistPanel.Children.Count];
		DecklistPanel.Children.CopyTo(children, 0);
		TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, new());
		await new ClientPacket.update_list(new() { Deck = new() { Cards = [.. Array.ConvertAll(children, child => (CardInfo)((Viewbox)((Button)child).Content!).DataContext!)], Ability = ability, Quest = quest, Player_class = playerClass, Name = (string)DeckSelectBox.SelectedItem! } }).WriteAsync(new TCompactProtocol(transport), default);
	}
	public async void DeleteDeckClick(object? sender, RoutedEventArgs args)
	{
		TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, new());
		await new ClientPacket.delete_list(new()).WriteAsync(new TCompactProtocol(transport), default);
		_ = DeckSelectBox.Items.Remove((string)DeckSelectBox.SelectedItem!);
		DeckSelectBox.SelectedIndex = DeckSelectBox.ItemCount - 1;
	}
	public async void SidebarTextInput(object? sender, KeyEventArgs args)
	{
		TextBox? tb = (TextBox?)sender;
		await LoadSidebar(tb?.Text ?? "");
	}

	public async Task LoadDecks()
	{
		TTransport transport = new TSocketTransport(host: Program.config.deck_edit_url.address, port: Program.config.deck_edit_url.port, config: new());
		await new ClientPacket.names(new()).WriteAsync(new TCompactProtocol(transport), default);
		ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
		List<string> names = packet.As_names!.Names ?? [];
		names.Sort();
		DeckSelectBox.Items.Clear();
		foreach(string name in names)
		{
			_ = DeckSelectBox.Items.Add(name);
		}
	}
}
