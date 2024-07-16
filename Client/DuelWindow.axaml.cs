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
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using CardGameUtils.Shared;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Duel;
using Google.FlatBuffers;

namespace CardGameClient;

public partial class DuelWindow : Window
{
	private readonly int playerIndex;
	private readonly TcpClient client;
	private readonly Stream stream;
	private readonly Task networkingTask;
	private readonly Flyout optionsFlyout = new();
	readonly Queue<ServerFieldUpdatePacket> fieldUpdateQueue = new();
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
	private void TrySend(byte[] packet)
	{
		if(client.Connected)
		{
			stream.Write(packet);
		}
		else
		{
			new ErrorPopup("Stream was closed").Show(this);
		}
	}
	private void PassClick(object? sender, RoutedEventArgs args)
	{
		TrySend(ClientPacketTToByteArray(new(){Content = new(){Type = ClientContent.pass, Value = new ClientPassPacketT()}}));
	}
	private async void HandleNetwork()
	{
		Log("Socketthread started");
		bool hasPassed = false;
		while(!closing)
		{
			if(client.Connected)
			{
				ServerPacket packet = await Task.Run(() => Functions.ReadSizedDuelServerPacketFromStream(stream)).ConfigureAwait(false);
				if(await Dispatcher.UIThread.InvokeAsync(() => HandlePacket(packet)))
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
		}
	}

	private bool HandlePacket(ServerPacket packet)
	{
		switch(packet.ContentType)
		{
			case ServerContent.fieldupdate:
			{
				EnqueueFieldUpdate(packet.ContentAsfieldupdate());
			}
			break;
			case ServerContent.yesno:
			{
				windowToShowAfterUpdate = new YesNoWindow(packet.ContentAsyesno().Question, stream);
			}
			break;
			case ServerContent.customselectcards:
			{
				windowToShowAfterUpdate = new CustomSelectCardsWindow(packet.ContentAscustomselectcards(), stream, playerIndex, ShowCard);
			}
			break;
			case ServerContent.getoptions:
			{
				UpdateCardOptions(packet.ContentAsgetoptions());
			}
			break;
			case ServerContent.selectzone:
			{
				windowToShowAfterUpdate = new SelectZoneWindow(packet.ContentAsselectzone().UnPack().Options, stream);
			}
			break;
			case ServerContent.gameresult:
			{
				windowToShowAfterUpdate = new GameResultWindow(this, packet.ContentAsgameresult());
			}
			break;
			case ServerContent.selectcards:
			{
				windowToShowAfterUpdate = new SelectCardsWindow(packet.ContentAsselectcards(), stream, playerIndex, ShowCard);
			}
			break;
			case ServerContent.showcards:
			{
				ServerShowCardsPacketT request = packet.ContentAsshowcards().UnPack();
				windowToShowAfterUpdate = new ViewCardsWindow(cards: request.Cards, message: request.Message, showCardAction: ShowCard);
			}
			break;
			default:
				throw new NotImplementedException($"{packet.GetType()}");
		}
		return false;
	}

	private void ShowCard(CardInfoT c)
	{
		UIUtils.CardHover(CardImagePanel, CardTextBlock, c, false);
	}

	private void UpdateCardOptions(ServerGetOptionsPacket response)
	{
		if(response.Location == CardGameUtils.Constants.Location.Hand)
		{
			foreach(Control b in OwnHandPanel.Children)
			{
				if(((CardInfoT)b.DataContext!).Uid == response.Uid)
				{
					if(response.OptionsLength == 0)
					{
						return;
					}
					StackPanel p = new();
					for(int i = 0; i < response.OptionsLength; i++)
					{
						CardAction? action = response.Options(i);
						if(action is null)
						{
							throw new Exception($"Somehow received a null option");
						}
						Button option = new()
						{
							Content = new TextBlock
							{
								Text = action.Value.Description
							}
						};
						option.Click += (_, _) => SendCardOption(action.Value.UnPack(), response.Uid, response.Location);
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
				if(((CardInfoT)b.DataContext).Uid == response.Uid)
				{
					StackPanel p = new();
					for(int i = 0; i < response.OptionsLength; i++)
					{
						CardAction? action = response.Options(i);
						if(action is null)
						{
							throw new Exception($"Somehow received a null option");
						}
						Button option = new()
						{
							Content = new TextBlock
							{
								Text = action.Value.Description
							}
						};
						option.Click += (_, _) => SendCardOption(action.Value.UnPack(), response.Uid, response.Location);
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
			for(int i = 0; i < response.OptionsLength; i++)
			{
				CardAction? action = response.Options(i);
				if(action is null)
				{
					throw new Exception($"Somehow received a null option");
				}
				Button option = new()
				{
					Content = new TextBlock
					{
						Text = action.Value.Description
					}
				};
				option.Click += (_, _) => SendCardOption(action.Value.UnPack(), response.Uid, response.Location);
			}
			optionsFlyout.Content = p;
			optionsFlyout.ShowAt(OwnQuestPanel, true);
		}
		else if(response.Location == CardGameUtils.Constants.Location.Ability)
		{
			StackPanel p = new();
			for(int i = 0; i < response.OptionsLength; i++)
			{
				CardAction? action = response.Options(i);
				if(action is null)
				{
					throw new Exception($"Somehow received a null option");
				}
				Button option = new()
				{
					Content = new TextBlock
					{
						Text = action.Value.Description
					}
				};
				option.Click += (_, _) => SendCardOption(action.Value.UnPack(), response.Uid, response.Location);
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
		TrySend(ClientPacketTToByteArray(new(){Content = new(){Type = ClientContent.viewgrave, Value = new ClientViewGravePacketT{OfOpponent = true}}}));
	}
	public void OwnGraveClick(object? sender, RoutedEventArgs args)
	{
		TrySend(ClientPacketTToByteArray(new(){Content = new(){Type = ClientContent.viewgrave, Value = new ClientViewGravePacketT{OfOpponent = false}}}));
	}
	private void SendCardOption(CardActionT action, int uid, CardGameUtils.Constants.Location location)
	{
		TrySend(ClientPacketTToByteArray(new()
		{
			Content = new()
			{
				Type = ClientContent.selectoption,
				Value = new ClientSelectOptionPacketT
				{
					Action = action,
					Location = location,
					Uid = uid,
				}
			}
		}));
	}

	public void EnqueueFieldUpdate(ServerFieldUpdatePacket request)
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
		ServerFieldUpdatePacket request = fieldUpdateQueue.Dequeue();
		string turnText = $"Turn {request.Turn}";
		if(TurnBlock.Text != turnText)
		{
			KeepPassingBox.IsChecked = false;
		}
		TurnBlock.Text = turnText;
		InitBlock.Text = request.HasInitiative ? "You have initiative" : "Your opponent has initiative";
		DirectionBlock.Text = "Battle direction: " + (request.BattleDirectionLeftToRight ? "->" : "<-");
		if(request.HasInitiative)
		{
			Background = Brushes.Purple;
		}
		else
		{
			ClearValue(BackgroundProperty);
		}
		shouldEnablePassButtonAfterUpdate = request.HasInitiative;
		if(request.Opp.HasValue)
		{
			OppNameBlock.Text = request.Opp.Value.Name;
			OppLifeBlock.Text = $"Life: {request.Opp.Value.Life}";
			OppMomentumBlock.Text = $"Momentum: {request.Opp.Value.Momentum}";
			OppDeckButton.Content = request.Opp.Value.DeckSize;
			OppGraveButton.Content = request.Opp.Value.GraveSize;
			if(request.Opp.Value.Ability.HasValue)
			{
				OppAbilityPanel.Children.Clear();
				OppAbilityPanel.Children.Add(CreateCardButton(request.Opp.Value.Ability.Value));
			}
			if(request.Opp.Value.Quest.HasValue)
			{
				OppQuestPanel.Children.Clear();
				OppQuestPanel.Children.Add(CreateCardButton(request.Opp.Value.Quest.Value));
			}
			Avalonia.Thickness oppBorderThickness = new(2, 2, 2, 0);
			PhaseBlock.Text = (request.MarkedZone != -1) ? "Battle Phase" : "Main Phase";
			if(request.Opp.Value.ShownInfo.HasValue)
			{
				TextBlock block = new() {Text = "Opp: "};
				if(request.Opp.Value.ShownInfo.Value.Card.HasValue)
				{
					block.Text += $"{request.Opp.Value.ShownInfo.Value.Card!.Value.Name}: ";
					block.PointerEntered += (sender, args) =>
					{
						if(sender == null || args.KeyModifiers.HasFlag(KeyModifiers.Control))
						{
							return;
						}
						UIUtils.CardHover(CardImagePanel, CardTextBlock, request.Opp.Value.ShownInfo.Value.Card!.Value.UnPack(), false);
						OppShowPanel.Children.Add(CreateCardButton(request.Opp.Value.ShownInfo.Value.Card!.Value));
					};
				}
				if(!string.IsNullOrWhiteSpace(request.Opp.Value.ShownInfo.Value.Description))
				{
					block.Text += request.Opp.Value.ShownInfo.Value.Description;
				}
				activities.Insert(0, block);
			}
			for(int i = 0; i < GameConstants.FIELD_SIZE; i++)
			{
				FieldCardInfo infoType = request.Opp.Value.FieldType(GameConstants.FIELD_SIZE - i - 1);
				if(infoType == FieldCardInfo.card)
				{
					CardInfo c = request.Opp.Value.Field<CardInfo>(GameConstants.FIELD_SIZE - i - 1)!.Value;
					Button b = CreateCardButton(c);
					if(request.MarkedZone != -1 && i == request.MarkedZone)
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
					if(request.MarkedZone != -1 && i == request.MarkedZone)
					{
						b.BorderBrush = Brushes.Yellow;
						b.BorderThickness = oppBorderThickness;
					}
					OppField.Children[i] = b;
				}
			}
			OppHandPanel.Children.Clear();
			List<CardInfoT> hand = request.Opp.Value.UnPack().Hand;
			UIUtils.CacheArtworkBatchFromServer(hand.ConvertAll(x => x.Name));
			for(int i = 0; i < request.Opp.Value.HandLength; i++)
			{
				OppHandPanel.Children.Add(CreateCardButton(request.Opp.Value.Hand(i)!.Value));
			}
			OppShowPanel.Children.Clear();
		}

		if(request.Own.HasValue)
		{
			OwnNameBlock.Text = request.Own.Value.Name;
			OwnLifeBlock.Text = $"Life: {request.Own.Value.Life}";
			OwnMomentumBlock.Text = $"Momentum: {request.Own.Value.Momentum}";
			OwnDeckButton.Content = request.Own.Value.DeckSize;
			OwnGraveButton.Content = request.Own.Value.GraveSize;
			if(request.Own.Value.Ability.HasValue)
			{
				OwnAbilityPanel.Children.Clear();
				OwnAbilityPanel.Children.Add(CreateCardButton(request.Own.Value.Ability.Value));
			}
			if(request.Own.Value.Quest.HasValue)
			{
				OwnQuestPanel.Children.Clear();
				OwnQuestPanel.Children.Add(CreateCardButton(request.Own.Value.Quest.Value));
			}
			Avalonia.Thickness oppBorderThickness = new(2, 2, 2, 0);
			PhaseBlock.Text = (request.MarkedZone != -1) ? "Battle Phase" : "Main Phase";
			if(request.Own.Value.ShownInfo.HasValue)
			{
				TextBlock block = new() {Text = "Own: "};
				if(request.Own.Value.ShownInfo.Value.Card.HasValue)
				{
					block.Text += $"{request.Own.Value.ShownInfo.Value.Card!.Value.Name}: ";
					block.PointerEntered += (sender, args) =>
					{
						if(sender == null || args.KeyModifiers.HasFlag(KeyModifiers.Control))
						{
							return;
						}
						UIUtils.CardHover(CardImagePanel, CardTextBlock, request.Own.Value.ShownInfo.Value.Card!.Value.UnPack(), false);
						OwnShowPanel.Children.Add(CreateCardButton(request.Own.Value.ShownInfo.Value.Card!.Value));
					};
				}
				if(!string.IsNullOrWhiteSpace(request.Own.Value.ShownInfo.Value.Description))
				{
					block.Text += request.Own.Value.ShownInfo.Value.Description;
				}
				activities.Insert(0, block);
			}
			for(int i = 0; i < GameConstants.FIELD_SIZE; i++)
			{
				FieldCardInfo infoType = request.Own.Value.FieldType(GameConstants.FIELD_SIZE - i - 1);
				if(infoType == FieldCardInfo.card)
				{
					CardInfo c = request.Own.Value.Field<CardInfo>(GameConstants.FIELD_SIZE - i - 1)!.Value;
					Button b = CreateCardButton(c);
					if(request.MarkedZone != -1 && i == request.MarkedZone)
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
					if(request.MarkedZone != -1 && i == request.MarkedZone)
					{
						b.BorderBrush = Brushes.Yellow;
						b.BorderThickness = oppBorderThickness;
					}
					OwnField.Children[i] = b;
				}
			}
			OwnHandPanel.Children.Clear();
			List<CardInfoT> hand = request.Own.Value.UnPack().Hand;
			UIUtils.CacheArtworkBatchFromServer(hand.ConvertAll(x => x.Name));
			for(int i = 0; i < request.Own.Value.HandLength; i++)
			{
				OwnHandPanel.Children.Add(CreateCardButton(request.Own.Value.Hand(i)!.Value));
			}
			OwnShowPanel.Children.Clear();
		}


		ActivityLogList.ItemsSource = activities;
	}

	private Button CreateCardButton(CardInfo card)
	{
		Button b = new()
		{
			DataContext = card,
			Background = (card.TypeSpecificsType == TypeSpecifics.quest && card.Text.Contains("REWARD CLAIMED")) ? Brushes.Green : null,
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
			UIUtils.CardHover(CardImagePanel, CardTextBlock, card.UnPack(), false);
		};
		if(card.Controller == playerIndex)
		{
			b.Click += (sender, args) =>
			{
				args.Handled = true;
				OptionsRequest(card.Location, card.Uid);
			};
		}
		if(card.TypeSpecificsType == TypeSpecifics.NONE)
		{
			b.Background = Brushes.DimGray;
		}
		else
		{
			b.Content = UIUtils.CreateGenericCard(card.UnPack());
		}
		return b;
	}

	private void OptionsRequest(CardGameUtils.Constants.Location location, int uid)
	{
		TrySend(ClientPacketTToByteArray(new(){Content = new(){Type = ClientContent.getoptions, Value = new ClientGetOptionsPacketT{Location = location, Uid = uid}}}));
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
				stream.Write(DuelWindow.ClientPacketTToByteArray(new(){Content = new() {Type = ClientContent.surrender, Value = new ClientSurrenderPacketT()}}));
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

	public static byte[] ClientPacketTToByteArray(ClientPacketT packet)
	{
		FlatBufferBuilder builder = new(1);
		builder.FinishSizePrefixed(ClientPacket.Pack(builder, packet).Value);
		return builder.DataBuffer.ToSizedArray();
	}
}
