using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CardGameUtils;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Duel;
using Thrift.Protocol;
using Thrift.Transport.Client;
using static CardGameUtils.Functions;

namespace CardGameClient;

public partial class DuelWindow : Window
{
	private readonly int playerIndex;
	private readonly TcpClient client;
	private readonly Stream stream;
	private readonly Task networkingTask;
	private readonly Flyout optionsFlyout = new();
	readonly Queue<ServerFieldUpdate> fieldUpdateQueue = new();
	private Task? fieldUpdateTask;
	private bool closing;
	private bool shouldEnablePassButtonAfterUpdate;
	private Window? windowToShowAfterUpdate;
	private readonly ObservableCollection<TextBlock> activities = [];

	// This constructor creates a completely empty duel window with no interaction possibility
	public DuelWindow()
	{
		InitializeComponent();
		client = new TcpClient();
		stream = new MemoryStream();
		networkingTask = new Task(() => { });
		OppField.LayoutUpdated += FieldInitialized;
		OwnField.LayoutUpdated += FieldInitialized;
	}
	public DuelWindow(int playerIndex, TcpClient client)
	{
		this.playerIndex = playerIndex;
		InitializeComponent();
		this.client = client;
		stream = client.GetStream();
		// TODO: Is this correct????
		networkingTask = HandleNetwork();
		networkingTask.Start();
		Closed += async (sender, args) =>
		{
			await Cleanup();
		};
		OppField.LayoutUpdated += FieldInitialized;
		OwnField.LayoutUpdated += FieldInitialized;
	}

	private void FieldInitialized(object? sender, EventArgs e)
	{
		if(sender == null)
		{
			return;
		}
		Panel panel = (Panel)sender;
		for(int i = 0; i < GameConstants.FIELD_SIZE; i++)
		{
			panel.Children.Add(new Button
			{
				Width = (panel.Bounds.Width - 10) / GameConstants.FIELD_SIZE,
				Height = panel.Bounds.Height - 10,
			});
		}
		panel.LayoutUpdated -= FieldInitialized;
	}

	private async void SurrenderClick(object? sender, RoutedEventArgs args)
	{
		new ServerWindow().Show();
		await Cleanup();
		Close();
	}
	private async Task TrySend(ClientPacket packet)
	{
		if(client.Connected)
		{
			await packet.WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
		}
		else
		{
			new ErrorPopup("Stream was closed").Show(this);
		}
	}
	private async void PassClick(object? sender, RoutedEventArgs args)
	{
		await TrySend(new ClientPacket.pass(new()));
	}
	private async Task HandleNetwork()
	{
		Log("Socketthread started");
		bool hasPassed = false;
		while(!closing)
		{
			if(client.Connected)
			{
				if(client.Available <= 0)
				{
					continue;
				}
				ServerPacket packet = await ServerPacket.ReadAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
				if(packet is not null && await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(packet)))
				{
					return;
				}
				if(fieldUpdateQueue.Count > 0)
				{
					hasPassed = false;
					if(fieldUpdateTask == null || fieldUpdateTask.IsCompleted)
					{
						await Dispatcher.UIThread.InvokeAsync(() =>
						{
							optionsFlyout.Hide();
							PassButton.IsEnabled = false;
						});
						fieldUpdateTask = Task.Delay(Program.config.animation_delay_in_ms).ContinueWith((_) => Dispatcher.UIThread.InvokeAsync(UpdateField));
					}
				}
				else
				{
					if(shouldEnablePassButtonAfterUpdate)
					{
						await Dispatcher.UIThread.InvokeAsync(async () =>
						{
							if(!hasPassed && (KeepPassingBox.IsChecked ?? false))
							{
								if(stream.CanWrite)
								{
									await TrySend(new ClientPacket.pass(new()));
									hasPassed = true;
								}
							}
							else
							{
								PassButton.IsEnabled = true;
							}
						});
					}
					if(windowToShowAfterUpdate != null)
					{
						await Dispatcher.UIThread.InvokeAsync(() =>
						{
							windowToShowAfterUpdate.Show();
							windowToShowAfterUpdate = null;
						});
					}
				}
			}
		}
	}

	private bool HandlePacket(ServerPacket packet)
	{
		switch(packet)
		{
			case ServerPacket.field_update:
			{
				EnqueueFieldUpdate(packet.As_field_update!);
			}
			break;
			case ServerPacket.yes_no:
			{
				windowToShowAfterUpdate = new YesNoWindow(packet.As_yes_no!.Question ?? "No question provided", client);
			}
			break;
			case ServerPacket.custom_select_cards:
			{
				ServerCustomSelectCards request = packet.As_custom_select_cards!;
				windowToShowAfterUpdate = new CustomSelectCardsWindow(request.Description ?? "No description provided", request.Cards!, request.Initial_state, client, playerIndex, ShowCard);
			}
			break;
			case ServerPacket.get_options:
			{
				UpdateCardOptions(packet.As_get_options);
			}
			break;
			case ServerPacket.select_zone:
			{
				windowToShowAfterUpdate = new SelectZoneWindow(packet.As_select_zone!.Possibilities!, client);
			}
			break;
			case ServerPacket.game_result:
			{
				windowToShowAfterUpdate = new GameResultWindow(this, packet.As_game_result!);
			}
			break;
			case ServerPacket.select_cards:
			{
				ServerSelectCards request = packet.As_select_cards!;
				windowToShowAfterUpdate = new SelectCardsWindow(request.Description ?? "No description provided", request.Amount, request.Cards!, client, playerIndex, ShowCard);
			}
			break;
			case ServerPacket.show_cards:
			{
				ServerShowCards request = packet.As_show_cards!;
				windowToShowAfterUpdate = new ViewCardsWindow(cards: request.Cards!, message: request.Description, showCardAction: ShowCard);
			}
			break;
			default:
				throw new NotImplementedException($"{packet.GetType()}");
		}
		return false;
	}

	private void ShowCard(CardInfo c)
	{
		_ = UIUtils.CardHover(CardImagePanel, CardTextBlock, c, false);
	}

	private void UpdateCardOptions(ServerGetOptions? response)
	{
		if(response?.Options is null || response.Options.Count == 0)
		{
			return;
		}
		if(response.Location == CardGameUtils.Constants.Location.Hand)
		{
			foreach(Control b in OwnHandPanel.Children)
			{
				if(((CardInfo)b.DataContext!).Uid == response.Uid)
				{
					StackPanel p = new();
					foreach(CardAction action in response.Options)
					{
						Button option = new()
						{
							Content = new TextBlock
							{
								Text = action.Description
							}
						};
						option.Click += async (_, _) => await SendCardOption(action, response.Uid, response.Location);
						p.Children.Add(option);
					}
					optionsFlyout.Content = p;
					optionsFlyout.ShowAt(b, true);
					return;
				}
			}
		}
		else if(response.Location == CardGameUtils.Constants.Location.Field)
		{
			foreach(Control b in OwnField.Children)
			{
				if(b.DataContext == null || b.DataContext == DataContext)
				{
					continue;
				}
				if(((CardInfo)b.DataContext).Uid == response.Uid)
				{
					StackPanel p = new();
					foreach(CardAction action in response.Options)
					{
						Button option = new()
						{
							Content = new TextBlock
							{
								Text = action.Description
							}
						};
						option.Click += async (_, _) => await SendCardOption(action, response.Uid, response.Location);
						p.Children.Add(option);
					}
					optionsFlyout.Content = p;
					optionsFlyout.ShowAt(b, true);
					return;
				}
			}
		}
		else if(response.Location == CardGameUtils.Constants.Location.Quest)
		{
			StackPanel p = new();
			foreach(CardAction action in response.Options)
			{
				Button option = new()
				{
					Content = new TextBlock
					{
						Text = action.Description
					}
				};
				option.Click += async (_, _) => await SendCardOption(action, response.Uid, response.Location);
				p.Children.Add(option);
			}
			optionsFlyout.Content = p;
			optionsFlyout.ShowAt(OwnQuestPanel, true);
		}
		else if(response.Location == CardGameUtils.Constants.Location.Ability)
		{
			StackPanel p = new();
			foreach(CardAction action in response.Options)
			{
				Button option = new()
				{
					Content = new TextBlock
					{
						Text = action.Description
					}
				};
				option.Click += async (_, _) => await SendCardOption(action, response.Uid, response.Location);
				p.Children.Add(option);
			}
			optionsFlyout.Content = p;
			optionsFlyout.ShowAt(OwnAbilityPanel, true);
		}
		else
		{
			throw new NotImplementedException($"Updating card options at {Enum.GetName(response.Location)}");
		}
	}

	public async void OppGraveClick(object? sender, RoutedEventArgs args)
	{
		await TrySend(new ClientPacket.show_grave(new() { Of_opponent = true }));
	}
	public async void OwnGraveClick(object? sender, RoutedEventArgs args)
	{
		await TrySend(new ClientPacket.show_grave(new() { Of_opponent = false }));
	}
	private async Task SendCardOption(CardAction action, int uid, CardGameUtils.Constants.Location location)
	{
		await TrySend(new ClientPacket.select_option(new()
		{
			Location = location,
			Uid = uid,
			Action = action
		}));
	}

	public void EnqueueFieldUpdate(ServerFieldUpdate request)
	{
		fieldUpdateQueue.Enqueue(request);
	}

	public async Task UpdateField()
	{
		Log($"{fieldUpdateQueue.Count}");
		if(fieldUpdateQueue.Count == 0)
		{
			return;
		}
		ServerFieldUpdate request = fieldUpdateQueue.Dequeue();
		string turnText = $"Turn {request.Turn}";
		if(TurnBlock.Text != turnText)
		{
			KeepPassingBox.IsChecked = false;
		}
		TurnBlock.Text = turnText;
		InitBlock.Text = request.Has_initiative ? "You have initiative" : "Your opponent has initiative";
		DirectionBlock.Text = "Battle direction: " + (request.Is_battle_direction_left_to_right ? "->" : "<-");
		PhaseBlock.Text = request.__isset.marked_zone ? "Battle Phase" : "Main Phase";
		if(request.Has_initiative)
		{
			Background = Brushes.Purple;
		}
		else
		{
			ClearValue(BackgroundProperty);
		}
		shouldEnablePassButtonAfterUpdate = request.Has_initiative;
		if(request.Opp is not null)
		{
			OppNameBlock.Text = request.Opp.Name;
			OppLifeBlock.Text = $"Life: {request.Opp.Life}";
			OppMomentumBlock.Text = $"Momentum: {request.Opp.Momentum}";
			OppDeckButton.Content = request.Opp.Deck_size;
			OppGraveButton.Content = request.Opp.Grave_size;
			if(request.Opp.Ability is not null)
			{
				OppAbilityPanel.Children.Clear();
				OppAbilityPanel.Children.Add(CreateCardButton(request.Opp.Ability));
			}
			if(request.Opp.Quest is not null)
			{
				OppQuestPanel.Children.Clear();
				OppQuestPanel.Children.Add(CreateCardButton(request.Opp.Quest));
			}
			Avalonia.Thickness oppBorderThickness = new(2, 2, 2, 0);
			if(request.Opp.Shown_info is not null)
			{
				TextBlock text = new() { Text = $"Opp: {request.Opp.Shown_info.Card?.Name}: {request.Opp.Shown_info.Description}" };
				text.PointerEntered += async (sender, args) =>
				{
					if(sender == null)
					{
						return;
					}
					if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
					{
						return;
					}
					if(request.Opp.Shown_info.Card is not null)
					{
						await UIUtils.CardHover(CardImagePanel, CardTextBlock, request.Opp.Shown_info.Card, false);
					}
					OppShowPanel.Children.Clear();
					if(request.Opp.Shown_info.Card is not null)
					{
						OppShowPanel.Children.Add(CreateCardButton(request.Opp.Shown_info.Card));
					}
				};
				activities.Insert(0, text);
			}
			if(request.Opp.Field is not null)
			{
				for(int i = 0; i < GameConstants.FIELD_SIZE; i++)
				{
					CardInfo? c = request.Opp.Field[GameConstants.FIELD_SIZE - i - 1].Info;
					if(c is not null)
					{
						Button b = CreateCardButton(c);
						if(request.__isset.marked_zone && i == request.Marked_zone)
						{
							b.BorderBrush = Brushes.Yellow;
							b.BorderThickness = oppBorderThickness;
						}
						OppField.Children[i] = b;
					}
					else
					{
						Button b = new()
						{
							Width = (OppField.Bounds.Width - 10) / GameConstants.FIELD_SIZE,
							Height = OppField.Bounds.Height - 10,
						};
						if(request.__isset.marked_zone && i == request.Marked_zone)
						{
							b.BorderBrush = Brushes.Yellow;
							b.BorderThickness = oppBorderThickness;
						}
						OppField.Children[i] = b;
					}
				}
			}
			if(request.Opp.Hand is not null)
			{
				OppHandPanel.Children.Clear();
				await UIUtils.CacheArtworkBatchFromServer(request.Opp.Hand.ConvertAll(x => x.Name!));
				for(int i = 0; i < request.Opp.Hand.Count; i++)
				{
					OppHandPanel.Children.Add(CreateCardButton(request.Opp.Hand[i]));
				}
			}
		}
		if(request.Own is not null)
		{
			OwnNameBlock.Text = request.Own.Name;
			OwnLifeBlock.Text = $"Life: {request.Own.Life}";
			OwnMomentumBlock.Text = $"Momentum: {request.Own.Momentum}";
			OwnDeckButton.Content = request.Own.Deck_size;
			OwnGraveButton.Content = request.Own.Grave_size;
			if(request.Own.Ability is not null)
			{
				OwnAbilityPanel.Children.Clear();
				OwnAbilityPanel.Children.Add(CreateCardButton(request.Own.Ability));
			}
			if(request.Own.Quest is not null)
			{
				OwnQuestPanel.Children.Clear();
				OwnQuestPanel.Children.Add(CreateCardButton(request.Own.Quest));
			}
			Avalonia.Thickness oppBorderThickness = new(2, 2, 2, 0);
			if(request.Own.Shown_info is not null)
			{
				TextBlock text = new() { Text = $"Own: {request.Own.Shown_info.Card?.Name}: {request.Own.Shown_info.Description}" };
				text.PointerEntered += async (sender, args) =>
				{
					if(sender == null)
					{
						return;
					}
					if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
					{
						return;
					}
					if(request.Own.Shown_info.Card is not null)
					{
						await UIUtils.CardHover(CardImagePanel, CardTextBlock, request.Own.Shown_info.Card, false);
					}
					OwnShowPanel.Children.Clear();
					if(request.Own.Shown_info.Card is not null)
					{
						OwnShowPanel.Children.Add(CreateCardButton(request.Own.Shown_info.Card));
					}
				};
				activities.Insert(0, text);
			}
			if(request.Own.Field is not null)
			{
				for(int i = 0; i < GameConstants.FIELD_SIZE; i++)
				{
					CardInfo? c = request.Own.Field[GameConstants.FIELD_SIZE - i - 1].Info;
					if(c is not null)
					{
						Button b = CreateCardButton(c);
						if(request.__isset.marked_zone && i == request.Marked_zone)
						{
							b.BorderBrush = Brushes.Yellow;
							b.BorderThickness = oppBorderThickness;
						}
						OwnField.Children[i] = b;
					}
					else
					{
						Button b = new()
						{
							Width = (OwnField.Bounds.Width - 10) / GameConstants.FIELD_SIZE,
							Height = OwnField.Bounds.Height - 10,
						};
						if(request.__isset.marked_zone && i == request.Marked_zone)
						{
							b.BorderBrush = Brushes.Yellow;
							b.BorderThickness = oppBorderThickness;
						}
						OwnField.Children[i] = b;
					}
				}
			}
			if(request.Own.Hand is not null)
			{
				OwnHandPanel.Children.Clear();
				await UIUtils.CacheArtworkBatchFromServer(request.Own.Hand.ConvertAll(x => x.Name!));
				for(int i = 0; i < request.Own.Hand.Count; i++)
				{
					OwnHandPanel.Children.Add(CreateCardButton(request.Own.Hand[i]));
				}
			}
		}
	}

	private Button CreateCardButton(CardInfo card)
	{
		Button b = new()
		{
			DataContext = card,
			Background = (card.Card_type_specifics is CardTypeSpecifics.quest && card.Text!.Contains("REWARD CLAIMED")) ? Brushes.Green : null,
		};
		if(card.Location != CardGameUtils.Constants.Location.Hand)
		{
			b.MinWidth = OwnField.Bounds.Width / GameConstants.FIELD_SIZE;
		}
		if(card.Location == CardGameUtils.Constants.Location.Field)
		{
			b.Width = (OwnField.Bounds.Width - 10) / GameConstants.FIELD_SIZE;
		}
		b.Height = OwnField.Bounds.Height - 10;
		b.PointerEntered += (sender, args) =>
		{
			if(sender == null)
			{
				return;
			}
			if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
			{
				return;
			}
			_ = UIUtils.CardHover(CardImagePanel, CardTextBlock, card, false);
		};
		if(card.Controller == playerIndex)
		{
			b.Click += async (sender, args) =>
			{
				args.Handled = true;
				await OptionsRequest(card.Location, card.Uid);
			};
		}
		if(card.Card_type_specifics is null)
		{
			b.Background = Brushes.DimGray;
		}
		else
		{
			b.Content = UIUtils.CreateGenericCard(card);
		}
		return b;
	}

	private async Task OptionsRequest(CardGameUtils.Constants.Location location, int uid)
	{
		await TrySend(new ClientPacket.get_options(new() { Location = location, Uid = uid }));
	}

	private async Task Cleanup()
	{
		if(closing)
		{
			return;
		}
		closing = true;
		Monitor.Enter(stream);
		if(client.Connected)
		{
			try
			{
				await new ClientPacket.surrender(new()).WriteAsync(new TCompactProtocol(new TSocketTransport(client, new())), default);
			}
			catch(Exception e)
			{
				Log($"Exception while sending cleanup message: {e}", severity: LogSeverity.Warning);
			}
		}
		Monitor.Exit(stream);
		networkingTask.Dispose();
		client.Close();
	}
}
