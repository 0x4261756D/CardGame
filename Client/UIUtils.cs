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
using CardGameUtils.Base;
using System;

namespace CardGameClient;

internal class UIUtils
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
		HashSet<string> filenames = [];
		foreach(string name in names)
		{
			string filename = Functions.CardnameToFilename(name);
			if(ArtworkCache.ContainsKey(filename))
			{
				continue;
			}
			_ = filenames.Add(filename);
		}
		if(filenames.Count == 0)
		{
			return;
		}
		CardGameUtils.Structs.Server.SToC_Response_Artworks? response = ServerWindow.TrySendAndReceive<CardGameUtils.Structs.Server.SToC_Content.artworks>(new CardGameUtils.Structs.Server.CToS_Content.artworks(new([.. filenames])),
			Program.config.server_address, GameConstants.SERVER_PORT)?.value;
		if(response is null || response.artworks.Count == 0)
		{
			_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
			return;
		}
		foreach(CardGameUtils.Structs.Server.Artwork artwork in response.artworks)
		{
			string filename = Functions.CardnameToFilename(artwork.name);
			if(filenames.Contains(filename))
			{
				string pathWithExtension = Path.Combine(Program.config.artwork_path, filename + Functions.ArtworkFiletypeToExtension(artwork.filetype));
				File.WriteAllBytes(pathWithExtension, [.. artwork.data]);
				Bitmap ret = new(new MemoryStream([.. artwork.data]));
				ArtworkCache[filename] = ret;
			}
			else
			{
				ArtworkCache[filename] = TryLoadArtworkFromDisk(filename);
			}
		}
		foreach(string filename in filenames)
		{
			if(!ArtworkCache.ContainsKey(filename))
			{
				ArtworkCache[filename] = null;
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
		if(ArtworkCache.TryGetValue(filename, out Bitmap? bitmap))
		{
			return bitmap;
		}
		Bitmap? fromDisk = TryLoadArtworkFromDisk(filename: filename);
		if(fromDisk is not null)
		{
			return fromDisk;
		}
		if(!ServersNotSupportingArtworks.Contains(Program.config.server_address))
		{
			try
			{
				List<CardGameUtils.Structs.Server.Artwork> response = ServerWindow.SendAndReceive<CardGameUtils.Structs.Server.SToC_Content.artworks>(new CardGameUtils.Structs.Server.CToS_Content.artworks(new([filename])), Program.config.server_address, GameConstants.SERVER_PORT).value.artworks;
				if(response.Count == 1)
				{
					CardGameUtils.Structs.Server.Artwork artwork = response[0];
					if(Functions.CardnameToFilename(artwork.name) == filename)
					{
						string pathWithExtension = Path.Combine(Program.config.artwork_path, filename) + Functions.ArtworkFiletypeToExtension(artwork.filetype);
						File.WriteAllBytes(pathWithExtension, [.. artwork.data]);
						Bitmap ret = new(new MemoryStream([.. artwork.data]));
						ArtworkCache[filename] = ret;
						return ret;
					}
				}
			}
			catch(Exception e)
			{
				_ = ServersNotSupportingArtworks.Add(Program.config.server_address);
				Functions.Log(e.Message);
			}
		}
		return null;
	}
	public static Viewbox CreateGenericCard(CardStruct c)
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
				Text = c.name,
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
		Bitmap? bitmap = FetchArtwork(c.name);
		Border imageBorder = new()
		{
			Child = new Viewbox
			{
				Child = bitmap is null ? InterpretNameAsWeirdSVG(c.name) : new Image
				{
					Source = bitmap,
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
				Text = c.text,
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
		switch(c.type_specifics)
		{
			case TypeSpecifics.creature cr:
			{
				CreatureSpecifics creature = cr.value;
				outsideBorder.Background = Brushes.Orange;
				Border costBorder = new()
				{
					Child = new TextBlock
					{
						Text = $"Cost: {creature.cost} Power/Life: {creature.power}/{creature.life}",
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
			case TypeSpecifics.spell s:
			{
				SpellSpecifics spell = s.value;
				outsideBorder.Background = Brushes.SkyBlue;
				Border costBorder = new()
				{
					Child = new TextBlock
					{
						Text = $"Cost: {spell.cost}",
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
			case TypeSpecifics.quest q:
			{
				QuestSpecifics quest = q.value;
				outsideBorder.Background = Brushes.Green;
				Border goalBorder = new()
				{
					Child = new TextBlock
					{
						Text = $"{quest.progress}/{quest.goal}",
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
	public static PathIcon InterpretNameAsWeirdSVG(string name)
	{
		(byte r, byte g, byte b) = WeirdSVGInterpreter.IntepretNameAsRGB(name);
		return new() { Data = StreamGeometry.Parse(WeirdSVGInterpreter.InterpretNameAsWeirdImage(name)), Background = new SolidColorBrush(Color.FromRgb(r, g, b)), Foreground = new SolidColorBrush(Color.FromRgb((byte)(~r + g), (byte)(~g ^ b), (byte)(~b + r))) };
	}
	public static List<uint> CardListBoxSelectionToUID(ListBox box)
	{
		List<uint> uids = new(box.SelectedItems?.Count ?? 0);
		for(int i = 0; i < (box.SelectedItems?.Count ?? 0); i++)
		{
			uids.Add(((CardStruct)box.SelectedItems?[i]!).uid);
		}
		return uids;
	}

	public static void CardHover(Panel CardImagePanel, TextBlock CardTextBlock, CardStruct c, bool includeInfoIrrelevantForDeckEdit)
	{
		CardImagePanel.Children.Clear();
		Viewbox v = CreateGenericCard(c);
		CardImagePanel.Children.Add(v);

		CardTextBlock.Text = Functions.FormatCardStruct(c, includeInfoIrrelevantForDeckEdit: includeInfoIrrelevantForDeckEdit);
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
