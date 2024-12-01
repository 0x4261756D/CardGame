using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CardGameUtils;
using CardGameUtils.Base;
using CardGameUtils.Structs.Duel;
using static CardGameUtils.Functions;

namespace CardGameClient;

public partial class DuelWindow : Window
{
	private readonly int playerIndex;
	private readonly TcpClient client;
	private readonly Stream stream;
	private readonly Mutex streamMutex;
	private readonly Task networkingTask;
	private readonly Flyout optionsFlyout = new();
	public interface IFieldUpdateOrInfo
	{
		public record FieldUpdate(SToC_Broadcast_FieldUpdate Value) : IFieldUpdateOrInfo;
		public record Info(SToC_Broadcast_ShowInfo Value) : IFieldUpdateOrInfo;
	}
	readonly Queue<IFieldUpdateOrInfo> fieldUpdateQueue = new();
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
		streamMutex = new Mutex();
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
		streamMutex = new Mutex();
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
		for(int i = 0; i < GameConstantsElectricBoogaloo.FIELD_SIZE; i++)
		{
			panel.Children.Add(new Button
			{
				Width = (panel.Bounds.Width - 10) / GameConstantsElectricBoogaloo.FIELD_SIZE,
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
	private void TrySend(CToS_Content content)
	{
		if(client.Connected)
		{
			_ = streamMutex.WaitOne();
			stream.Write(new CToS_Packet(content).Serialize());
			streamMutex.ReleaseMutex();
		}
		else
		{
			new ErrorPopup("Stream was closed").Show(this);
		}
	}
	public SToC_Content? TryReceivePacket(NetworkStream stream, int timeoutInMs)
	{
		try
		{
			if(streamMutex.WaitOne(timeoutInMs))
			{
				Task task = Task.Run(() => { while(!stream.DataAvailable) { } });
				int i = Task.WaitAny(task, Task.Delay(timeoutInMs));
				SToC_Content? ret = i == 0 ? SToC_Packet.Deserialize(stream).content : null;
				streamMutex.ReleaseMutex();
				return ret;
			}
			return null;
		}
		catch(Exception e)
		{
			Log(e.Message, severity: LogSeverity.Warning);
			return null;
		}
	}
	private void PassClick(object? sender, RoutedEventArgs args)
	{
		TrySend(new CToS_Content.pass());
	}
	private async void HandleNetwork()
	{
		Log("Socketthread started");
		bool hasPassed = false;
		while(!closing)
		{
			if(client.Connected)
			{
				SToC_Content? content = await Task.Run(() => TryReceivePacket((NetworkStream)stream, 100)).ConfigureAwait(false);
				if(content != null && await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(content)))
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
			Thread.Sleep(10);
		}
	}

	private bool HandlePacket(SToC_Content content)
	{
		switch(content)
		{
			case SToC_Content.field_update request:
			{
				EnqueueFieldUpdateOrInfo(new IFieldUpdateOrInfo.FieldUpdate(request.value));
			}
			break;
			case SToC_Content.show_info request:
			{
				EnqueueFieldUpdateOrInfo(new IFieldUpdateOrInfo.Info(request.value));
			}
			break;
			case SToC_Content.yes_no request:
			{
				Log("Received a yesno requets", severity: LogSeverity.Error);
				windowToShowAfterUpdate = new YesNoWindow(request.value.question, stream, streamMutex);
			}
			break;
			case SToC_Content.select_cards_custom r:
			{
				SToC_Request_SelectCardsCustom request = r.value;
				windowToShowAfterUpdate = new CustomSelectCardsWindow(request.description!, request.cards, request.initial_state, stream, streamMutex, playerIndex, ShowCard);
			}
			break;
			case SToC_Content.get_actions request:
			{
				UpdateCardOptions(request.value);
			}
			break;
			case SToC_Content.select_zone request:
			{
				windowToShowAfterUpdate = new SelectZoneWindow(request.value.options, stream, streamMutex);
			}
			break;
			case SToC_Content.game_result request:
			{
				windowToShowAfterUpdate = new GameResultWindow(this, request.value);
			}
			break;
			case SToC_Content.select_cards r:
			{
				SToC_Request_SelectCards request = r.value;
				windowToShowAfterUpdate = new SelectCardsWindow(request.description, request.amount, request.cards, stream, streamMutex, playerIndex, ShowCard);
			}
			break;
			case SToC_Content.show_cards r:
			{
				SToC_Response_ShowCards request = r.value;
				windowToShowAfterUpdate = new ViewCardsWindow(cards: request.cards, message: request.description, showCardAction: ShowCard);
			}
			break;
			default:
				throw new NotImplementedException($"{content.GetType()}");
		}
		return false;
	}

	private void ShowCard(CardStruct c)
	{
		UIUtils.CardHover(CardImagePanel, CardTextBlock, c, true);
	}

	private void UpdateCardOptions(SToC_Response_GetActions response)
	{
		if(response.location == CardGameUtils.GameConstants.Location.Hand)
		{
			foreach(Control b in OwnHandPanel.Children)
			{
				if(((CardStruct)b.DataContext!).uid == response.uid)
				{
					if(response.actions.Count == 0)
					{
						return;
					}
					StackPanel p = new();
					foreach(CardAction action in response.actions)
					{
						Button option = new()
						{
							Content = new TextBlock
							{
								Text = action.description
							}
						};
						option.Click += (_, _) => SendCardOption(action, response.uid, response.location);
						p.Children.Add(option);
					}
					optionsFlyout.Content = p;
					optionsFlyout.ShowAt(b, true);
					return;
				}
			}
		}
		else if(response.location == CardGameUtils.GameConstants.Location.Field)
		{
			foreach(Control b in OwnField.Children)
			{
				if(b.DataContext == null || b.DataContext == DataContext)
				{
					continue;
				}
				if(((CardStruct)b.DataContext).uid == response.uid)
				{
					StackPanel p = new();
					foreach(CardAction action in response.actions)
					{
						Button option = new()
						{
							Content = new TextBlock
							{
								Text = action.description
							}
						};
						option.Click += (_, _) => SendCardOption(action, response.uid, response.location);
						p.Children.Add(option);
					}
					optionsFlyout.Content = p;
					optionsFlyout.ShowAt(b, true);
					return;
				}
			}
		}
		else if(response.location == CardGameUtils.GameConstants.Location.Quest)
		{
			StackPanel p = new();
			foreach(CardAction action in response.actions)
			{
				Button option = new()
				{
					Content = new TextBlock
					{
						Text = action.description
					}
				};
				option.Click += (_, _) => SendCardOption(action, response.uid, response.location);
				p.Children.Add(option);
			}
			optionsFlyout.Content = p;
			optionsFlyout.ShowAt(OwnQuestPanel, true);
		}
		else if(response.location == CardGameUtils.GameConstants.Location.Ability)
		{
			StackPanel p = new();
			foreach(CardAction action in response.actions)
			{
				Button option = new()
				{
					Content = new TextBlock
					{
						Text = action.description
					}
				};
				option.Click += (_, _) => SendCardOption(action, response.uid, response.location);
				p.Children.Add(option);
			}
			optionsFlyout.Content = p;
			optionsFlyout.ShowAt(OwnAbilityPanel, true);
		}
		else
		{
			throw new NotImplementedException($"Updating card options at {Enum.GetName(response.location)}");
		}
	}

	public void OppGraveClick(object? sender, RoutedEventArgs args)
	{
		TrySend(new CToS_Content.view_grave(new(for_opponent: true)));
	}
	public void OwnGraveClick(object? sender, RoutedEventArgs args)
	{
		TrySend(new CToS_Content.view_grave(new(for_opponent: false)));
	}
	private void SendCardOption(CardAction action, uint uid, CardGameUtils.GameConstants.Location location)
	{
		TrySend(new CToS_Content.select_option(new
		(
			location: location,
			uid: uid,
			action: action
		)));
	}

	public void EnqueueFieldUpdateOrInfo(IFieldUpdateOrInfo request)
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
		switch(fieldUpdateQueue.Dequeue())
		{
			case IFieldUpdateOrInfo.FieldUpdate r:
			{
				SToC_Broadcast_FieldUpdate request = r.Value;
				string turnText = $"Turn {request.turn}";
				if(TurnBlock.Text != turnText)
				{
					KeepPassingBox.IsChecked = false;
				}
				TurnBlock.Text = turnText;
				InitBlock.Text = request.has_initiative ? "You have initiative" : "Your opponent has initiative";
				DirectionBlock.Text = "Battle direction: " + (request.is_battle_direction_left_to_right ? "->" : "<-");
				if(request.has_initiative)
				{
					Background = Brushes.Purple;
				}
				else
				{
					ClearValue(BackgroundProperty);
				}
				shouldEnablePassButtonAfterUpdate = request.has_initiative;

				OppNameBlock.Text = request.opp_field.name;
				OppLifeBlock.Text = $"Life: {request.opp_field.life}";
				OppMomentumBlock.Text = $"Momentum: {request.opp_field.momentum}";
				OppDeckButton.Content = request.opp_field.deck_size;
				OppGraveButton.Content = request.opp_field.grave_size;
				OppAbilityPanel.Children.Clear();
				OppAbilityPanel.Children.Add(CreateCardButton(request.opp_field.ability));
				OppQuestPanel.Children.Clear();
				OppQuestPanel.Children.Add(CreateCardButton(request.opp_field.quest));
				Avalonia.Thickness oppBorderThickness = new(2, 2, 2, 0);
				PhaseBlock.Text = (request.marked_zone != null) ? "Battle Phase" : "Main Phase";
				for(int i = 0; i < GameConstantsElectricBoogaloo.FIELD_SIZE; i++)
				{
					CardStruct? c = request.opp_field.field[GameConstantsElectricBoogaloo.FIELD_SIZE - i - 1];
					if(c != null)
					{
						Button b = CreateCardButton(c);
						if(request.marked_zone != null && i == request.marked_zone)
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
							Width = (OppField.Bounds.Width - 10) / GameConstantsElectricBoogaloo.FIELD_SIZE,
							Height = OppField.Bounds.Height - 10,
						};
						if(request.marked_zone != null && i == request.marked_zone)
						{
							b.BorderBrush = Brushes.Yellow;
							b.BorderThickness = oppBorderThickness;
						}
						OppField.Children[i] = b;
					}
				}
				OppHandPanel.Children.Clear();
				UIUtils.CacheArtworkBatchFromServer(request.opp_field.hand.ConvertAll(x => x.name));
				for(int i = 0; i < request.opp_field.hand.Count; i++)
				{
					OppHandPanel.Children.Add(CreateCardButton(request.opp_field.hand[i]));
				}


				OwnNameBlock.Text = request.own_field.name;
				OwnLifeBlock.Text = $"Life: {request.own_field.life}";
				OwnMomentumBlock.Text = $"Momentum: {request.own_field.momentum}";
				OwnDeckButton.Content = request.own_field.deck_size;
				OwnGraveButton.Content = request.own_field.grave_size;
				OwnAbilityPanel.Children.Clear();
				OwnAbilityPanel.Children.Add(CreateCardButton(request.own_field.ability));
				OwnQuestPanel.Children.Clear();
				OwnQuestPanel.Children.Add(CreateCardButton(request.own_field.quest));
				Avalonia.Thickness ownBorderThickness = new(2, 0, 2, 2);
				for(int i = 0; i < GameConstantsElectricBoogaloo.FIELD_SIZE; i++)
				{
					CardStruct? c = request.own_field.field[i];
					if(c != null)
					{
						Button b = CreateCardButton(c);
						if(request.marked_zone != null && i == request.marked_zone)
						{
							b.BorderBrush = Brushes.Yellow;
							b.BorderThickness = ownBorderThickness;
						}
						OwnField.Children[i] = b;
					}
					else
					{
						Button b = new()
						{
							Width = (OppField.Bounds.Width - 10) / GameConstantsElectricBoogaloo.FIELD_SIZE,
							Height = OppField.Bounds.Height - 10,
						};
						if(request.marked_zone != null && i == request.marked_zone)
						{
							b.BorderBrush = Brushes.Yellow;
							b.BorderThickness = ownBorderThickness;
						}
						OwnField.Children[i] = b;
					}
				}
				OwnHandPanel.Children.Clear();
				UIUtils.CacheArtworkBatchFromServer(request.own_field.hand.ConvertAll(x => x.name));
				for(int i = 0; i < request.own_field.hand.Count; i++)
				{
					OwnHandPanel.Children.Add(CreateCardButton(request.own_field.hand[i]));
				}
			}
			break;
			case IFieldUpdateOrInfo.Info r:
			{
				SToC_Broadcast_ShowInfo request = r.Value;
				if(request.player == playerIndex)
				{
					OwnShowPanel.Children.Clear();
					if(request.info is not null)
					{
						StringBuilder text = new("You");
						TextBlock block = new();
						if(request.info.card is not null)
						{
							_ = text.Append(": ").Append(request.info.card.name);
							OwnShowPanel.Children.Add(CreateCardButton(request.info.card));
							block.PointerEntered += (sender, args) =>
							{
								if(sender == null)
								{
									return;
								}
								if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
								{
									return;
								}
								UIUtils.CardHover(CardImagePanel, CardTextBlock, request.info.card, true);
								OwnShowPanel.Children.Add(CreateCardButton(request.info.card));
							};
							block.PointerExited += (sender, args) =>
							{
								if(sender is null)
								{
									return;
								}
								OwnShowPanel.Children.Clear();
							};
						}
						if(request.info.description is not null)
						{
							_ = text.Append(": ").Append(request.info.description);
						}
						block.Text = text.ToString();
						activities.Insert(0, block);
					}
				}
				else
				{
					OppShowPanel.Children.Clear();
					if(request.info is not null)
					{
						StringBuilder text = new("Opp");
						TextBlock block = new();
						if(request.info.card is not null)
						{
							_ = text.Append(": ").Append(request.info.card.name);
							OppShowPanel.Children.Add(CreateCardButton(request.info.card));
							block.PointerEntered += (sender, args) =>
							{
								if(sender == null)
								{
									return;
								}
								if(args.KeyModifiers.HasFlag(KeyModifiers.Control))
								{
									return;
								}
								UIUtils.CardHover(CardImagePanel, CardTextBlock, request.info.card, true);
								OppShowPanel.Children.Add(CreateCardButton(request.info.card));
							};
							block.PointerExited += (sender, args) =>
							{
								if(sender is null)
								{
									return;
								}
								OppShowPanel.Children.Clear();
							};
						}
						if(request.info.description is not null)
						{
							_ = text.Append(": ").Append(request.info.description);
						}
						block.Text = text.ToString();
						activities.Insert(0, block);
					}
				}
			}
			break;
		}
		ActivityLogList.ItemsSource = activities;
	}

	private Button CreateCardButton(CardStruct card)
	{
		Button b = new()
		{
			DataContext = card,
			Background = (card.type_specifics is TypeSpecifics.quest && card.text.Contains("REWARD CLAIMED")) ? Brushes.Green : null,
		};
		if(card.location != CardGameUtils.GameConstants.Location.Hand)
		{
			b.MinWidth = OwnField.Bounds.Width / GameConstantsElectricBoogaloo.FIELD_SIZE;
		}
		if(card.location == CardGameUtils.GameConstants.Location.Field)
		{
			b.Width = (OwnField.Bounds.Width - 10) / GameConstantsElectricBoogaloo.FIELD_SIZE;
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
			UIUtils.CardHover(CardImagePanel, CardTextBlock, card, true);
		};
		if(card.controller == playerIndex)
		{
			b.Click += (sender, args) =>
			{
				args.Handled = true;
				OptionsRequest(card.location, card.uid);
			};
		}
		if(card.type_specifics is TypeSpecifics.unknown)
		{
			b.Background = Brushes.DimGray;
		}
		else
		{
			b.Content = UIUtils.CreateGenericCard(card);
		}
		return b;
	}

	private void OptionsRequest(CardGameUtils.GameConstants.Location location, uint uid)
	{
		TrySend(new CToS_Content.get_actions(new(location: location, uid: uid)));
	}

	private void Cleanup()
	{
		if(closing)
		{
			return;
		}
		closing = true;
		if(client.Connected)
		{
			try
			{
				_ = streamMutex.WaitOne();
				stream.Write(new CToS_Packet(new CToS_Content.surrender()).Serialize());
				streamMutex.ReleaseMutex();
			}
			catch(Exception e)
			{
				Log($"Exception while sending cleanup message: {e}", severity: LogSeverity.Warning);
			}
		}
		networkingTask.Dispose();
		client.Close();
	}
}
