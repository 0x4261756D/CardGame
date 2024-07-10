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
using CardGameUtils.Constants;
using CardGameUtils.Structs;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;

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
#pragma warning disable CA2007
			await using Stream stream = await files[0].OpenReadAsync();
#pragma warning restore CA2007
			using StreamReader reader = new(stream);
			return await reader.ReadToEndAsync().ConfigureAwait(true);
		}
		return null;
	}

	private static readonly Dictionary<string, Bitmap?> ArtworkCache = [];
	private static Bitmap? DefaultArtwork;
	private static readonly HashSet<string> ServersNotSupportingArtworks = [];
	public static async Task CacheArtworkBatchFromServer(List<string> names)
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
		try
		{
			TTransport transport = new TSocketTransport(host: Program.config.server_address, port: GenericConstants.SERVER_PORT, timeout: 100, config: new());
			await new CardGameUtils.Packets.Server.ClientPacket.artworks(new()
			{
				Names = filenames
			}).WriteAsync(new TCompactProtocol(transport), default);
			CardGameUtils.Packets.Server.ServerPacket packet = await CardGameUtils.Packets.Server.ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
			CardGameUtils.Packets.Server.ServerArtworks response = packet.As_artworks!;
			if(!response.Supports_artworks)
			{
				_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
				return;
			}
			foreach(string filename in filenames)
			{
				if((response.Artworks?.TryGetValue(Functions.CardnameToFilename(filename), out CardGameUtils.Packets.Server.ArtworkInfo? artwork) ?? false) && artwork.Data is not null)
				{
					string pathWithExtension = Path.Combine(Program.config.artwork_path, filename + Functions.ArtworkFiletypeToExtension(artwork.Filetype));
					await File.WriteAllBytesAsync(pathWithExtension, artwork.Data);
					Bitmap ret = new(pathWithExtension);
					ArtworkCache[filename] = ret;
				}
				else
				{
					ArtworkCache[filename] = TryLoadArtworkFromDisk(filename);
				}
			}
		}
		catch(Exception)
		{
			return;
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
	public static async Task<Bitmap?> FetchArtwork(string? name)
	{
		if(name is null)
		{
			return null;
		}
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
			try
			{
				TTransport transport = new TSocketTransport(host: Program.config.server_address, port: GenericConstants.SERVER_PORT, config: new());
				await new CardGameUtils.Packets.Server.ClientPacket.artworks(new()
				{
					Names = [filename]
				}).WriteAsync(new TCompactProtocol(transport), default);
				CardGameUtils.Packets.Server.ServerPacket packet = await CardGameUtils.Packets.Server.ServerPacket.ReadAsync(new TCompactProtocol(transport), default);
				CardGameUtils.Packets.Server.ServerArtworks response = packet.As_artworks!;
				if(!response.Supports_artworks)
				{
					_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
				}
				else if((response.Artworks?.TryGetValue(filename, out CardGameUtils.Packets.Server.ArtworkInfo? artwork) ?? false) && artwork.Data is not null)
				{
					string pathWithExtension = Path.Combine(Program.config.artwork_path, filename) + Functions.ArtworkFiletypeToExtension(artwork.Filetype);
					await File.WriteAllBytesAsync(pathWithExtension, artwork.Data);
					Bitmap ret = new(pathWithExtension);
					ArtworkCache[filename] = ret;
					return ret;
				}
			}
			catch(Exception e)
			{
				Functions.Log(e.Message);
			}
		}
		if(DefaultArtwork == null && File.Exists(Path.Combine(Program.config.artwork_path, "default_artwork.png")))
		{
			DefaultArtwork = new Bitmap(Path.Combine(Program.config.artwork_path, "default_artwork.png"));
		}
		return DefaultArtwork;
	}
	public static async Task<Viewbox> CreateGenericCard(CardInfo c)
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
					Source = await FetchArtwork(c.Name),
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
		switch(c.Card_type_specifics)
		{
			case CardTypeSpecifics.creature:
			{
				CreatureSpecifics specifics = c.Card_type_specifics.As_creature!;
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
			case CardTypeSpecifics.spell:
			{
				SpellSpecifics specifics = c.Card_type_specifics.As_spell!;
				outsideBorder.Background = Brushes.SkyBlue;
				Border costBorder = new()
				{
					Child = new TextBlock
					{
						Text = $"Cost: {specifics.Cost}",
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
			case CardTypeSpecifics.quest:
			{
				QuestSpecifics specifics = c.Card_type_specifics.As_quest!;
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
		List<int> uids = new(box.SelectedItems?.Count ?? 0);
		for(int i = 0; i < (box.SelectedItems?.Count ?? 0); i++)
		{
			uids[i] = ((CardInfo)box.SelectedItems?[i]!).Uid;
		}
		return uids;
	}

	public static async Task CardHover(Panel CardImagePanel, TextBlock CardTextBlock, CardInfo c, bool inDeckEdit)
	{
		CardImagePanel.Children.Clear();
		Viewbox v = await CreateGenericCard(c);
		CardImagePanel.Children.Add(v);

		CardTextBlock.Text = Functions.FormatCardInfo(c, inDeckEdit: inDeckEdit);
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
					if(!possibleKeyword.Contains(' ') && ClientConstants.KeywordDescriptions.TryGetValue(possibleKeyword, out string? value))
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
