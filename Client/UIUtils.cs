using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CardGameUtils;
using Google.FlatBuffers;
using CardGameUtils.Structs;
using System.Net.Sockets;
using CardGameUtils.Constants;
using CardGameUtils.Shared;

namespace CardGameClient;

public class UIUtils
{
	public static ThemeVariant ConvertThemeVariant(ClientConfig.ThemeVariant? theme)
	{
		return theme switch
		{
			ClientConfig.ThemeVariant.Default => ThemeVariant.Default,
			ClientConfig.ThemeVariant.Dark => ThemeVariant.Dark,
			ClientConfig.ThemeVariant.Light => ThemeVariant.Light,
			_ => ThemeVariant.Default,
		};
	}

	// public static R? TrySendAndReceive<R>(Packet request, string address, int port, Window? window) where R : Packet
	// {
	// 	try
	// 	{
	// 		return Functions.SendAndReceive<R>(request, address, port);
	// 	}
	// 	catch(Exception ex)
	// 	{
	// 		if(window != null && window.IsVisible)
	// 		{
	// 			new ErrorPopup(ex.Message).Show(window);
	// 		}
	// 		else
	// 		{
	// 			new ErrorPopup(ex.Message).Show();
	// 		}
	// 		return null;
	// 	}
	// }

	public static async Task<string?> SelectFileAsync(Window window, string title = "Select file", bool allowMultiple = false)
	{
		TopLevel? topLevel = TopLevel.GetTopLevel(window);
		if(topLevel == null)
		{
			return null;
		}
		IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = title,
			AllowMultiple = allowMultiple,
		}).ConfigureAwait(true);
		if(files.Count > 0)
		{
			return files[0].Path.AbsolutePath;
		}
		return null;
	}
	public static async Task<string?> SelectAndReadFileAsync(Window window, string title = "Select file", bool allowMultiple = false)
	{
		TopLevel? topLevel = TopLevel.GetTopLevel(window);
		if(topLevel == null)
		{
			return null;
		}
		IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = title,
			AllowMultiple = allowMultiple,
		}).ConfigureAwait(true);
		if(files.Count > 0)
		{
			using Stream stream = await files[0].OpenReadAsync().ConfigureAwait(true);
			using StreamReader reader = new(stream);
			return await reader.ReadToEndAsync().ConfigureAwait(true);
		}
		return null;
	}

	private static readonly Dictionary<string, Bitmap?> ArtworkCache = [];
	private static Bitmap? DefaultArtwork;
	private static readonly HashSet<string> ServersNotSupportingArtworks = [];
	public static void CacheArtworkBatchFromServer(List<string> names)
	{
		if(ServersNotSupportingArtworks.Contains(Program.config.server_address))
		{
			return;
		}
		if(Program.config.artwork_path is null)
		{
			return;
		}
		List<string> filenames = [];
		foreach(string name in names)
		{
			string filename = Functions.CardnameToFilename(name);
			if(ArtworkCache.ContainsKey(filename))
			{
				continue;
			}
			filenames.Add(filename);
		}
		if(filenames.Count == 0)
		{
			return;
		}
		FlatBufferBuilder builder = new(1);
		builder.FinishSizePrefixed(CardGameUtils.Packets.Server.ClientPacket.Pack(builder, new()
		{
			Content = new()
			{
				Type = CardGameUtils.Packets.Server.ClientContent.artworks,
				Value = new CardGameUtils.Packets.Server.ClientArtworksPacketT
				{
					Names = filenames
				}
			}
		}).Value);
		CardGameUtils.Packets.Server.ServerPacket packet;
		try
		{
			using TcpClient client = new(Program.config.server_address, InternalConstants.SERVER_PORT);
			using NetworkStream stream = client.GetStream();
			stream.Write(builder.DataBuffer.ToSizedArray());
			packet = Functions.ReadSizedServerServerPacketFromStream(stream);
		}
		catch(Exception e)
		{
			Functions.Log(e.Message);
			ServersNotSupportingArtworks.Add(Program.config.server_address);
			return;
		}
		if(packet.ContentType != CardGameUtils.Packets.Server.ServerContent.artworks)
		{
			Functions.Log($"Expected packet of type `artworks` but got {packet.ContentType}", severity: Functions.LogSeverity.Warning);
			_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
			return;
		}
		CardGameUtils.Packets.Server.ServerArtworksPacket response = packet.ContentAsartworks();
		if(!response.SupportsArtworks)
		{
			_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
			return;
		}
		for(int i = 0; i < response.ArtworksLength; i++)
		{
			CardGameUtils.Packets.Server.ArtworkInfo? info = response.Artworks(i);
			if(info.HasValue && filenames.Contains(info.Value.Name))
			{
				if(info.Value.Filetype != CardGameUtils.Packets.Server.ArtworkFiletype.UNKNOWN && info.Value.DataLength > 0)
				{
					string pathWithExtension = Path.Combine(Program.config.artwork_path, info.Value.Name + Functions.ArtworkToFiletypeExtension(info.Value.Filetype));
					File.WriteAllBytes(pathWithExtension, info.Value.GetDataArray());
					ArtworkCache[info.Value.Name] = new(pathWithExtension);
				}
				else
				{
					ArtworkCache[info.Value.Name] = TryLoadArtworkFromDisk(info.Value.Name);
				}
			}
		}
	}
	public static Bitmap? TryLoadArtworkFromDisk(string filename)
	{
		if(Program.config.artwork_path is null)
		{
			return null;
		}
		string pathNoExtension = Path.Combine(Program.config.artwork_path, filename);
		if(File.Exists(pathNoExtension + ".png"))
		{
			Bitmap ret = new(pathNoExtension + ".png");
			ArtworkCache[filename] = ret;
			return ret;
		}
		if(File.Exists(pathNoExtension + ".jpg"))
		{
			Bitmap ret = new(pathNoExtension + ".jpg");
			ArtworkCache[filename] = ret;
			return ret;
		}
		return null;
	}
	public static Bitmap? FetchArtwork(string name)
	{
		string filename = Functions.CardnameToFilename(name);
		if(Program.config.artwork_path == null)
		{
			return null;
		}
		Bitmap? fromDisk = TryLoadArtworkFromDisk(filename: filename);
		if(ArtworkCache.TryGetValue(filename, out Bitmap? bitmap))
		{
			if(bitmap is null)
			{
				if(DefaultArtwork == null && File.Exists(Path.Combine(Program.config.artwork_path, "default_artwork.png")))
				{
					DefaultArtwork = new Bitmap(Path.Combine(Program.config.artwork_path, "default_artwork.png"));
				}
				return DefaultArtwork;
			}
			return bitmap;
		}
		if(fromDisk is not null)
		{
			return fromDisk;
		}
		if(!ServersNotSupportingArtworks.Contains(Program.config.server_address))
		{
			FlatBufferBuilder builder = new(1);
			builder.FinishSizePrefixed(CardGameUtils.Packets.Server.ClientPacket.Pack(builder, new()
			{
				Content = new()
				{
					Type = CardGameUtils.Packets.Server.ClientContent.artworks,
					Value = new CardGameUtils.Packets.Server.ClientArtworksPacketT
					{
						Names = [filename]
					}
				}
			}).Value);
			using TcpClient client = new(Program.config.server_address, InternalConstants.SERVER_PORT);
			using NetworkStream stream = client.GetStream();
			stream.Write(builder.DataBuffer.ToSizedArray());
			CardGameUtils.Packets.Server.ServerPacket packet = Functions.ReadSizedServerServerPacketFromStream(stream);
			if(packet.ContentType != CardGameUtils.Packets.Server.ServerContent.artworks)
			{
				Functions.Log($"Expected packet of type `artworks` but got {packet.ContentType}", severity: Functions.LogSeverity.Warning);
				_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
				return null;
			}
			CardGameUtils.Packets.Server.ServerArtworksPacket response = packet.ContentAsartworks();
			if(!response.SupportsArtworks)
			{
				_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
				return null;
			}
			CardGameUtils.Packets.Server.ArtworkInfo? info = response.Artworks(0);
			if(info.HasValue && filename == info.Value.Name)
			{
				if(info.Value.Filetype != CardGameUtils.Packets.Server.ArtworkFiletype.UNKNOWN && info.Value.DataLength > 0)
				{
					string pathWithExtension = Path.Combine(Program.config.artwork_path, info.Value.Name + Functions.ArtworkToFiletypeExtension(info.Value.Filetype));
					File.WriteAllBytes(pathWithExtension, info.Value.GetDataArray());
					ArtworkCache[info.Value.Name] = new(pathWithExtension);
				}
			}
		}
		if(DefaultArtwork == null && File.Exists(Path.Combine(Program.config.artwork_path, "default_artwork.png")))
		{
			DefaultArtwork = new Bitmap(Path.Combine(Program.config.artwork_path, "default_artwork.png"));
		}
		return DefaultArtwork;
	}
	public static Viewbox CreateGenericCard(CardInfoT c)
	{
		Viewbox box = new()
		{
			Stretch = Stretch.Uniform,
		};
		RelativePanel insidePanel = new()
		{
			Width = 1000,
			Height = 1500,
		};
		Border outsideBorder = new()
		{
			Child = insidePanel,
			BorderBrush = Brushes.Black,
			BorderThickness = new Thickness(5),
		};
		Border headerBorder = new()
		{
			Child = new TextBlock
			{
				Text = c.Name,
				FontSize = 50,
				TextAlignment = TextAlignment.Center,
			},
			Margin = new Thickness(30),
			Padding = new Thickness(10),
			Background = Brushes.Gray,
			BorderBrush = Brushes.Black,
			BorderThickness = new Thickness(3),
			CornerRadius = new CornerRadius(10),
		};
		insidePanel.Children.Add(headerBorder);
		RelativePanel.SetAlignLeftWithPanel(headerBorder, true);
		RelativePanel.SetAlignRightWithPanel(headerBorder, true);
		Border imageBorder = new()
		{
			Child = new Viewbox
			{
				Child = new Image
				{
					Source = FetchArtwork(c.Name),
				},
			},
			Margin = new Thickness(50, 0),
			BorderBrush = Brushes.Black,
			BorderThickness = new Thickness(3),
		};
		insidePanel.Children.Add(imageBorder);
		RelativePanel.SetBelow(imageBorder, headerBorder);
		Border textBorder = new()
		{
			Child = new TextBlock
			{
				Text = c.Text,
				TextWrapping = TextWrapping.Wrap,
				FontSize = 40,
				Foreground = Brushes.White,
				Margin = new Thickness(0, 0, 0, 100)
			},
			Margin = new Thickness(40),
			BorderBrush = Brushes.Black,
			BorderThickness = new Thickness(3),
			CornerRadius = new CornerRadius(30),
			Padding = new Thickness(20),
			Background = Brush.Parse("#515151")
		};
		insidePanel.Children.Add(textBorder);
		RelativePanel.SetBelow(textBorder, imageBorder);
		RelativePanel.SetAlignLeftWithPanel(textBorder, true);
		RelativePanel.SetAlignRightWithPanel(textBorder, true);
		switch(c.TypeSpecifics.Type)
		{
			case TypeSpecifics.creature:
			{
				CreatureSpecificsT specifics = c.TypeSpecifics.Ascreature();
				outsideBorder.Background = Brushes.Orange;
				Border costBorder = new()
				{
					Child = new TextBlock
					{
						Text = $"Cost: {specifics.Cost} Power/Life: {specifics.Power}/{specifics.Life}",
						FontSize = 50,
						TextAlignment = TextAlignment.Center,
						Margin = new Thickness(20),
					},
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(3),
					Margin = new Thickness(30),
					Background = Brushes.Gray,
				};
				insidePanel.Children.Add(costBorder);
				RelativePanel.SetAlignBottomWith(costBorder, textBorder);
			}
			break;
			case TypeSpecifics.spell:
			{
				outsideBorder.Background = Brushes.SkyBlue;
				Border costBorder = new()
				{
					Child = new TextBlock
					{
						Text = $"Cost: {c.TypeSpecifics.Asspell().Cost}",
						FontSize = 50,
						TextAlignment = TextAlignment.Center,
						Margin = new Thickness(20),
					},
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(3),
					Margin = new Thickness(30),
					Background = Brushes.Gray,
				};
				insidePanel.Children.Add(costBorder);
				RelativePanel.SetAlignBottomWith(costBorder, textBorder);
			}
			break;
			case TypeSpecifics.quest:
			{
				QuestSpecificsT specifics = c.TypeSpecifics.Asquest();
				outsideBorder.Background = Brushes.Green;
				Border goalBorder = new()
				{
					Child = new TextBlock
					{
						Text = $"{specifics.Progress}/{specifics.Goal}",
						FontSize = 50,
						TextAlignment = TextAlignment.Center,
						Margin = new Thickness(20),
					},
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(3),
					Margin = new Thickness(30),
					Background = Brushes.Gray,
				};
				insidePanel.Children.Add(goalBorder);
				RelativePanel.SetAlignBottomWith(goalBorder, textBorder);
			}
			break;
		}
		RelativePanel.SetAlignBottomWithPanel(textBorder, true);
		box.Child = outsideBorder;
		box.DataContext = c;
		return box;
	}
	public static List<int> CardListBoxSelectionToUID(ListBox box)
	{
		List<int> uids = new List<int>(box.SelectedItems?.Count ?? 0);
		for(int i = 0; i < (box.SelectedItems?.Count ?? 0); i++)
		{
			uids[i] = ((CardInfoT)box.SelectedItems?[i]!).Uid;
		}
		return uids;
	}

	public static void CardHover(Panel CardImagePanel, TextBlock CardTextBlock, CardInfoT c, bool inDeckEdit)
	{
		CardImagePanel.Children.Clear();
		Viewbox v = CreateGenericCard(c);
		CardImagePanel.Children.Add(v);

		CardTextBlock.Text = Functions.CardInfoTToString(c, inDeckEdit: inDeckEdit);
		CardTextBlock.PointerMoved += CardTextHover;
	}
	private static void CardTextHover(object? sender, PointerEventArgs e)
	{
		if(sender is null)
		{
			return;
		}
		TextBlock block = (TextBlock)sender;
		if(block.Text is not null)
		{
			TextLayout layout = block.TextLayout;
			Point pointerPoint = e.GetPosition(block);
			TextHitTestResult hitTestResult = layout.HitTestPoint(pointerPoint);
			int position = hitTestResult.TextPosition;
			if(position >= 0 && position < block.Text.Length)
			{
				int start = block.Text.LastIndexOf('[', position);
				int end = block.Text.IndexOf(']', position);
				if(start >= 0 && end >= 0 && end < block.Text.Length && start != end)
				{
					string possibleKeyword = block.Text.Substring(start + 1, end - start - 1);
					if(!possibleKeyword.Contains(' ') && InternalConstants.KeywordDescriptions.TryGetValue(possibleKeyword, out string? value))
					{
						string description = value;
						ToolTip.SetTip(block, description);
						ToolTip.SetIsOpen(block, true);
						ToolTip.SetShowDelay(block, 0);
						return;
					}
				}
			}
		}
		_ = block.SetValue(ToolTip.IsOpenProperty, false);
	}

}
