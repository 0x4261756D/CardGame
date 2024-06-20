using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using CardGameUtils;
using CardGameUtils.CardConstants;
using Google.Protobuf;

namespace CardGameClient;

public partial class DeckEditWindow : Window
{
	private readonly Flyout moveFlyout = new();
	private readonly List<CardInfo> cardpool = [];

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
		using(TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port))
		{
			PlayerClass playerClass = (PlayerClass?)ClassSelectBox.SelectedItem ?? PlayerClass.All;
			new CardGameUtils.DeckClientToServer.Packet
			{
				Search = new()
				{
					Filter = fil,
					PlayerClass = playerClass,
					IncludeGenericCards = SidebarGenericIncludeBox.IsChecked ?? false,
				}
			}.WriteDelimitedTo(client.GetStream());
			cardpool.Clear();
			cardpool.AddRange(CardGameUtils.DeckServerToClient.Search.Parser.ParseDelimitedFrom(client.GetStream()).Cards);
		}
		UIUtils.CacheArtworkBatchFromServer([.. cardpool.ConvertAll(x => x.Name)]);
		List<Control> items = [];
		foreach(CardInfo c in cardpool)
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
		CardInfo c = (CardInfo)((Control)sender).DataContext!;
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
		CardInfo card = (CardInfo)v.DataContext!;
		if(card.CardTypeCase == CardInfo.CardTypeOneofCase.Quest)
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
		Array.Sort(children, (child1, child2) => ((CardInfo)((Control)((Button)child1).Content!).DataContext!).Name.CompareTo(((CardInfo)((Control)((Button)child2).Content!).DataContext!).Name));
		DecklistPanel.Children.Clear();
		DecklistPanel.Children.AddRange(children);
	}

	public Button CreateDeckButton(CardInfo c)
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
		CardInfo c = (CardInfo)((Viewbox)button.Content!).DataContext!;
		if(c.CardTypeCase == CardInfo.CardTypeOneofCase.Spell && c.Spell.CanBeClassAbility)
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
		new CardGameUtils.DeckClientToServer.Packet
		{
			GetDecklist = new()
			{
				Name = deckName
			}
		}.WriteDelimitedTo(stream);
		Deck deck = CardGameUtils.DeckServerToClient.GetDecklist.Parser.ParseDelimitedFrom(stream).Deck;
		if(deck.PlayerClass == PlayerClass.Unknown)
		{
			ClassSelectBox.SelectedIndex = -1;
		}
		else
		{
			ClassSelectBox.SelectedItem = deck.PlayerClass;
		}
		string[] names = new string[deck.Cards.Count];
		for(int i = 0; i < names.Length; i++)
		{
			names[i] = deck.Cards[i].Name;
		}
		UIUtils.CacheArtworkBatchFromServer(names);
		foreach(CardInfo c in deck.Cards)
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
			PlayerClass cardClass = ((CardInfo)((Viewbox)child.Content!).DataContext!).CardClass;
			if(cardClass != PlayerClass.All && playerClass != PlayerClass.All && cardClass != playerClass)
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
			PlayerClass cardClass = ((CardInfo)((Viewbox)ClassQuestButton.Content).DataContext!).CardClass;
			if(cardClass != PlayerClass.All && playerClass != PlayerClass.All && cardClass != playerClass)
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
			PlayerClass cardClass = ((CardInfo)((Viewbox)ClassAbilityButton.Content).DataContext!).CardClass;
			if(cardClass != PlayerClass.All && playerClass != PlayerClass.All && cardClass != playerClass)
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
		new CardGameUtils.DeckClientToServer.Packet
		{
			UpdateDecklist = new()
			{
				Deck = new()
				{
					Name = newName
				}
			}
		}.WriteDelimitedTo(stream);
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
		PlayerClass playerClass = (PlayerClass?)ClassSelectBox.SelectedItem ?? PlayerClass.Unknown;
		if(playerClass == PlayerClass.All)
		{
			playerClass = PlayerClass.Unknown;
		}
		Viewbox? abilityBox = (Viewbox?)ClassAbilityButton.Content;
		CardInfo? ability = abilityBox == null ? null : (CardInfo?)abilityBox.DataContext;
		Viewbox? questBox = (Viewbox?)ClassQuestButton.Content;
		CardInfo? quest = questBox == null ? null : (CardInfo?)questBox.DataContext;
		Control[] children = new Control[DecklistPanel.Children.Count];
		DecklistPanel.Children.CopyTo(children, 0);
		using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		Deck deck = new()
		{
			Name = (string)DeckSelectBox.SelectedItem!,
			Ability = ability,
			Quest = quest,
			PlayerClass = playerClass,
		};
		deck.Cards.AddRange(Array.ConvertAll(children, child => (CardInfo)((Viewbox)((Button)child).Content!).DataContext!));
		new CardGameUtils.DeckClientToServer.Packet
		{
			UpdateDecklist = new()
			{
				Deck = deck
			}
		}.WriteDelimitedTo(stream);
	}
	public void DeleteDeckClick(object? sender, RoutedEventArgs args)
	{
		using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		new CardGameUtils.DeckClientToServer.Packet
		{
			DeleteDecklist = new()
			{
				Name =(string)DeckSelectBox.SelectedItem!
			}
		}.WriteDelimitedTo(stream);
		_ = ((DeckEditWindowViewModel)DataContext!).Decknames.Remove((string)DeckSelectBox.SelectedItem!);
		DeckSelectBox.SelectedIndex = DeckSelectBox.ItemCount - 1;
	}
	public void SidebarTextInput(object? sender, KeyEventArgs args)
	{
		TextBox? tb = (TextBox?)sender;
		LoadSidebar(tb?.Text ?? "");
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
		using TcpClient client = new(Program.config.deck_edit_url.address, Program.config.deck_edit_url.port);
		using NetworkStream stream = client.GetStream();
		new CardGameUtils.DeckClientToServer.Packet
		{
			Names = new()
		}.WriteDelimitedTo(stream);
		List<string> names = [];
		names.AddRange(CardGameUtils.DeckServerToClient.Names.Parser.ParseDelimitedFrom(stream).Names_);
		names.Sort();
		Decknames.Clear();
		foreach(string name in names)
		{
			Decknames.Add(name);
		}
		_ = classes.Remove(PlayerClass.Unknown);
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
