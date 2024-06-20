using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using CardGameUtils.CardConstants;
using CardGameUtils.DuelServerToClient;
using Google.Protobuf;
using static CardGameUtils.Functions;

namespace CardGameClient;

public partial class DuelWindow : Window
{
	private readonly int playerIndex;
	private readonly TcpClient client;
	private readonly Stream stream;
	private readonly Task networkingTask;
	private readonly Flyout optionsFlyout = new();
	readonly Queue<FieldUpdate> fieldUpdateQueue = new();
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
		networkingTask = new Task(HandleNetwork, TaskCreationOptions.LongRunning);
		networkingTask.Start();
		Closed += (sender, args) =>
		{
			Cleanup();
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

	private void SurrenderClick(object? sender, RoutedEventArgs args)
	{
		new ServerWindow().Show();
		Cleanup();
		Close();
	}
	private void TrySend(CardGameUtils.DuelClientToServer.Packet packet)
	{
		if(client.Connected)
		{
			packet.WriteDelimitedTo(stream);
		}
		else
		{
			new ErrorPopup("Stream was closed").Show(this);
		}
	}
	private void PassClick(object? sender, RoutedEventArgs args)
	{
		TrySend(new CardGameUtils.DuelClientToServer.Packet { Pass = new() });
	}
	private async void HandleNetwork()
	{
		Log("Socketthread started");
		bool hasPassed = false;
		while(!closing)
		{
			if(client.Connected)
			{
				Packet? packet = await Task.Run(() => TryReceivePacket((NetworkStream)stream, 100)).ConfigureAwait(false);
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
						await Dispatcher.UIThread.InvokeAsync(() =>
						{
							if(!hasPassed && (KeepPassingBox.IsChecked ?? false))
							{
								if(stream.CanWrite)
								{
									PassClick(null, new RoutedEventArgs());
									hasPassed = true;
								}
							}
							else
							{
								PassButton.IsEnabled = true;
							}
						});
					}
					if(windowToShowAfterUpdate is not null)
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

	private static async Task<Packet?>? TryReceivePacket(NetworkStream stream, int timeoutMilliseconds)
	{
		if(!stream.CanRead)
		{
			return null;
		}
		Stopwatch watch = Stopwatch.StartNew();
		while(!stream.DataAvailable)
		{
			await Task.Delay(10).ConfigureAwait(false);
			if(!stream.CanRead || (timeoutMilliseconds != -1 && timeoutMilliseconds < watch.ElapsedMilliseconds))
			{
				return null;
			}
		}
		return Packet.Parser.ParseDelimitedFrom(stream);
	}

	private bool HandlePacket(Packet packet)
	{
		switch(packet.KindCase)
		{
			case Packet.KindOneofCase.FieldUpdate:
			{
				EnqueueFieldUpdate(packet.FieldUpdate);
			}
			break;
			case Packet.KindOneofCase.YesNo:
			{
				Log("Received a yesno requets", severity: LogSeverity.Error);
				windowToShowAfterUpdate = new YesNoWindow(packet.YesNo.Question, stream);
			}
			break;
			case Packet.KindOneofCase.CustomSelectCards:
			{
				CustomSelectCards response = packet.CustomSelectCards;
				windowToShowAfterUpdate = new CustomSelectCardsWindow(response.Description, response.Cards, response.InitialState, stream, playerIndex, ShowCard);
			}
			break;
			case Packet.KindOneofCase.GetOptions:
			{
				UpdateCardOptions(packet.GetOptions);
			}
			break;
			case Packet.KindOneofCase.SelectZone:
			{
				windowToShowAfterUpdate = new SelectZoneWindow([.. packet.SelectZone.Options], stream);
			}
			break;
			case Packet.KindOneofCase.GameResult:
			{
				windowToShowAfterUpdate = new GameResultWindow(this, packet.GameResult.Value);
			}
			break;
			case Packet.KindOneofCase.SelectCards:
			{
				SelectCards request = packet.SelectCards;
				windowToShowAfterUpdate = new SelectCardsWindow(request.Description, request.Amount, request.Cards, stream, playerIndex, ShowCard);
			}
			break;
			case Packet.KindOneofCase.ShowCards:
			{
				ShowCards request = packet.ShowCards;
				windowToShowAfterUpdate = new ViewCardsWindow(cards: request.Cards, message: request.Message, showCardAction: ShowCard);
			}
			break;
			default:
				throw new NotImplementedException($"{packet.GetType()}");
		}
		return false;
	}

	private void ShowCard(CardInfo c)
	{
		UIUtils.CardHover(CardImagePanel, CardTextBlock, c, false);
	}

	private void UpdateCardOptions(GetOptions response)
	{
		if(response.Location == CardGameUtils.CardConstants.Location.Hand)
		{
			foreach(Control b in OwnHandPanel.Children)
			{
				if(((CardInfo)b.DataContext!).Uid == response.Uid)
				{
					if(response.Options.Count == 0)
					{
						return;
					}
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
						option.Click += (_, _) => SendCardOption(action, response.Uid, response.Location);
						p.Children.Add(option);
					}
					optionsFlyout.Content = p;
					optionsFlyout.ShowAt(b, true);
					return;
				}
			}
		}
		else if(response.Location == CardGameUtils.CardConstants.Location.Field)
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
						option.Click += (_, _) => SendCardOption(action, response.Uid, response.Location);
						p.Children.Add(option);
					}
					optionsFlyout.Content = p;
					optionsFlyout.ShowAt(b, true);
					return;
				}
			}
		}
		else if(response.Location == CardGameUtils.CardConstants.Location.Quest)
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
				option.Click += (_, _) => SendCardOption(action, response.Uid, response.Location);
				p.Children.Add(option);
			}
			optionsFlyout.Content = p;
			optionsFlyout.ShowAt(OwnQuestPanel, true);
		}
		else if(response.Location == CardGameUtils.CardConstants.Location.Ability)
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
				option.Click += (_, _) => SendCardOption(action, response.Uid, response.Location);
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

	public void OppGraveClick(object? sender, RoutedEventArgs args)
	{
		TrySend(new() { ViewGrave = new() { OfOpponent = true } });
	}
	public void OwnGraveClick(object? sender, RoutedEventArgs args)
	{
		TrySend(new() { ViewGrave = new() { OfOpponent = false } });
	}
	private void SendCardOption(CardAction action, int uid, CardGameUtils.CardConstants.Location location)
	{
		TrySend(new()
		{
			SelectOption = new()
			{
				Action = action,
				Uid = uid,
				Location = location,
			}
		});
	}

	public void EnqueueFieldUpdate(FieldUpdate request)
	{
		fieldUpdateQueue.Enqueue(request);
	}

	public void UpdateField()
	{
		Log($"{fieldUpdateQueue.Count}");
		if(fieldUpdateQueue.Count == 0)
		{
			return;
		}
		FieldUpdate request = fieldUpdateQueue.Dequeue();
		string turnText = $"Turn {request.Turn}";
		if(TurnBlock.Text != turnText)
		{
			KeepPassingBox.IsChecked = false;
		}
		TurnBlock.Text = turnText;
		InitBlock.Text = request.HasInitiative ? "You have initiative" : "Your opponent has initiative";
		DirectionBlock.Text = "Battle direction: " + (request.IsBattleDirectionLeftToRight ? "->" : "<-");
		if(request.HasInitiative)
		{
			Background = Brushes.Purple;
		}
		else
		{
			ClearValue(BackgroundProperty);
		}
		shouldEnablePassButtonAfterUpdate = request.HasInitiative;
		PhaseBlock.Text = request.HasMarkedZone ? "Battle Phase" : "Main Phase";
		OppNameBlock.Text = request.OppField.Name;
		OppLifeBlock.Text = $"Life: {request.OppField.Life}";
		OppMomentumBlock.Text = $"Momentum: {request.OppField.Momentum}";
		OppDeckButton.Content = request.OppField.DeckSize;
		OppGraveButton.Content = request.OppField.GraveSize;
		OppAbilityPanel.Children.Clear();
		OppAbilityPanel.Children.Add(CreateCardButton(request.OppField.Ability));
		OppQuestPanel.Children.Clear();
		OppQuestPanel.Children.Add(CreateCardButton(request.OppField.Quest));
		Avalonia.Thickness oppBorderThickness = new(2, 2, 2, 0);
		if(request.OppField.ShownInfo is not null && request.OppField.ShownInfo.HasDescription)
		{
			TextBlock textBlock = new() { Text = $"Opp: {request.OppField.ShownInfo.Card?.Name}: {request.OppField.ShownInfo.Description}" };
			textBlock.PointerEntered += (sender, args) =>
			{
				if(sender == null)
				{
					return;
				}
				if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
				{
					return;
				}
				if(request.OppField.ShownInfo.Card is not null)
				{
					UIUtils.CardHover(CardImagePanel, CardTextBlock, request.OppField.ShownInfo.Card, false);
				}
			};
			activities.Insert(0, textBlock);
		}
		if(request.OppField.Field_ is not null)
		{
			for(int i = 0; i < GameConstants.FIELD_SIZE; i++)
			{
				if(request.OppField.Field_.TryGetValue(i, out CardInfo? info) && info is not null)
				{
					Button b = CreateCardButton(info);
					if(request.HasMarkedZone && i == request.MarkedZone)
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
					if(request.HasMarkedZone && i == request.MarkedZone)
					{
						b.BorderBrush = Brushes.Yellow;
						b.BorderThickness = oppBorderThickness;
					}
					OppField.Children[i] = b;
				}
			}
		}
		OppHandPanel.Children.Clear();
		UIUtils.CacheArtworkBatchFromServer(Array.ConvertAll([.. request.OppField.Hand], x => x.Name));
		for(int i = 0; i < request.OppField.Hand.Count; i++)
		{
			OppHandPanel.Children.Add(CreateCardButton(request.OppField.Hand[i]));
		}
		OppShowPanel.Children.Clear();
		if(request.OppField.ShownInfo?.Card is not null)
		{
			OppShowPanel.Children.Add(CreateCardButton(request.OppField.ShownInfo.Card));
		}
		OwnNameBlock.Text = request.OwnField.Name;
		OwnLifeBlock.Text = $"Life: {request.OwnField.Life}";
		OwnMomentumBlock.Text = $"Momentum: {request.OwnField.Momentum}";
		OwnDeckButton.Content = request.OwnField.DeckSize;
		OwnGraveButton.Content = request.OwnField.GraveSize;
		OwnAbilityPanel.Children.Clear();
		OwnAbilityPanel.Children.Add(CreateCardButton(request.OwnField.Ability));
		OwnQuestPanel.Children.Clear();
		OwnQuestPanel.Children.Add(CreateCardButton(request.OwnField.Quest));
		Avalonia.Thickness ownBorderThickness = new(2, 2, 2, 0);
		if(request.OwnField.ShownInfo is not null && request.OwnField.ShownInfo.HasDescription)
		{
			TextBlock textBlock = new() { Text = $"Own: {request.OwnField.ShownInfo.Card?.Name}: {request.OwnField.ShownInfo.Description}" };
			textBlock.PointerEntered += (sender, args) =>
			{
				if(sender == null)
				{
					return;
				}
				if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
				{
					return;
				}
				if(request.OwnField.ShownInfo.Card is not null)
				{
					UIUtils.CardHover(CardImagePanel, CardTextBlock, request.OwnField.ShownInfo.Card, false);
				}
			};
			activities.Insert(0, textBlock);
		}
		if(request.OwnField.Field_ is not null)
		{
			for(int i = 0; i < GameConstants.FIELD_SIZE; i++)
			{
				if(request.OwnField.Field_.TryGetValue(i, out CardInfo info))
				{
					Button b = new()
					{
						Width = (OppField.Bounds.Width - 10) / GameConstants.FIELD_SIZE,
						Height = OppField.Bounds.Height - 10,
					};
					if(request.HasMarkedZone && i == request.MarkedZone)
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
					if(request.HasMarkedZone && i == request.MarkedZone)
					{
						b.BorderBrush = Brushes.Yellow;
						b.BorderThickness = ownBorderThickness;
					}
					OwnField.Children[i] = b;
				}
			}
		}
		OwnHandPanel.Children.Clear();
		UIUtils.CacheArtworkBatchFromServer(Array.ConvertAll([.. request.OwnField.Hand], x => x.Name));
		for(int i = 0; i < request.OwnField.Hand.Count; i++)
		{
			OwnHandPanel.Children.Add(CreateCardButton(request.OwnField.Hand[i]));
		}
		OwnShowPanel.Children.Clear();
		if(request.OwnField.ShownInfo?.Card is not null)
		{
			OwnShowPanel.Children.Add(CreateCardButton(request.OwnField.ShownInfo.Card));
		}

		ActivityLogList.ItemsSource = activities;
	}

	private Button CreateCardButton(CardInfo card)
	{
		Button b = new()
		{
			DataContext = card,
			Background = (card.CardTypeCase == CardInfo.CardTypeOneofCase.Quest && card.Text.Contains("REWARD CLAIMED")) ? Brushes.Green : null,
		};
		if(!card.Location.HasFlag(CardGameUtils.CardConstants.Location.Hand))
		{
			b.MinWidth = OwnField.Bounds.Width / GameConstants.FIELD_SIZE;
		}
		if(card.Location == CardGameUtils.CardConstants.Location.Field)
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
			UIUtils.CardHover(CardImagePanel, CardTextBlock, card, false);
		};
		if(card.Controller == playerIndex)
		{
			b.Click += (sender, args) =>
			{
				args.Handled = true;
				OptionsRequest(card.Location, card.Uid);
			};
		}
		if(card.CardTypeCase == CardInfo.CardTypeOneofCase.None)
		{
			b.Background = Brushes.DimGray;
		}
		else
		{
			b.Content = UIUtils.CreateGenericCard(card);
		}
		return b;
	}

	private void OptionsRequest(CardGameUtils.CardConstants.Location location, int uid)
	{
		TrySend(new() { GetOptions = new() { Location = location, Uid = uid } });
	}

	private void Cleanup()
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
				new CardGameUtils.DuelClientToServer.Packet { Surrender = new() }.WriteDelimitedTo(stream);
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
