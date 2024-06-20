using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CardGameUtils.CardConstants;

namespace CardGameUtils;

partial class Functions
{
	public enum LogSeverity
	{
		Debug,
		Warning,
		Error,
	}

	[GeneratedRegex(@"[^#\|a-zA-Z0-9]")]
	private static partial Regex CardnameFilenameRegex();
	public static string CardnameToFilename(string name)
	{
		return CardnameFilenameRegex().Replace(name, "");
	}

	[GeneratedRegex(@"[^a-zA-Z0-9]")]
	private static partial Regex UsernameFilenameRegex();
	public static string UsernameToFilename(string name)
	{
		return UsernameFilenameRegex().Replace(name, "_");
	}

	public static void Log(string message, LogSeverity severity = LogSeverity.Debug, bool includeFullPath = false, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string propertyName = "")
	{
		ConsoleColor current = Console.ForegroundColor;
		if(severity == LogSeverity.Warning)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
		}
		else if(severity == LogSeverity.Error)
		{
			Console.ForegroundColor = ConsoleColor.Red;
		}
#if RELEASE
		if(severity != LogSeverity.Debug)
		{
#endif
		Console.WriteLine($"{severity.ToString().ToUpper()}: [{(includeFullPath ? propertyName : Path.GetFileNameWithoutExtension(propertyName))}:{lineNumber}]: {message}");
#if RELEASE
		}
#endif
		Console.ForegroundColor = current;
	}

	public static string ArtworkFiletypeToExtension(ServerServerToClient.Artworks.Types.Filetype filetype)
	{
		return filetype switch
		{
			ServerServerToClient.Artworks.Types.Filetype.Jpg => ".jpg",
			ServerServerToClient.Artworks.Types.Filetype.Png => ".png",
			_ => throw new NotImplementedException(),
		};
	}

	public static string FormatCardInfo(CardInfo? info, bool inDuel = false, char separator = '\n')
	{
		if(info is null)
		{
			return "Card was null";
		}
		if(info.CardTypeCase == CardInfo.CardTypeOneofCase.None)
		{
			return "UNKNOWN";
		}
		StringBuilder builder = new();
		if(inDuel)
		{
			_ = builder.Append("UID: ").Append(info.Uid).Append(separator);
		}
		_ = builder.Append("Name: ").Append(info.Name).Append(separator);
		if(info.CardTypeCase == CardInfo.CardTypeOneofCase.Quest)
		{
			_ = builder.Append(separator).Append("Quest Progress: ").Append(info.Quest.Progress).Append('/').Append(info.Quest.Goal);
		}
		else if(info.Location == Location.Ability)
		{
			_ = builder.Append(separator).Append("Cost: 1");
		}
		else
		{
			_ = builder.Append(separator).Append("Cost: ").Append((info.CardTypeCase == CardInfo.CardTypeOneofCase.Creature) ? info.Creature.Cost : info.Spell.Cost);
			if(inDuel)
			{
				_ = builder.Append('/').Append((info.CardTypeCase == CardInfo.CardTypeOneofCase.Creature) ? info.Creature.BaseCost : info.Spell.BaseCost);
			}
		}
		if(inDuel)
		{
			_ = builder.Append(separator).Append("Controller: ").Append(info.Controller).Append('/').Append(info.BaseController);
		}
		_ = builder.Append(separator).Append("Card Type: ").Append(info.CardTypeCase).Append(separator).Append("Class: ").Append(info.CardClass);
		if(inDuel)
		{
			_ = builder.Append(separator).Append("Location: ").Append(info.Location);
		}
		if(info.CardTypeCase == CardInfo.CardTypeOneofCase.Creature)
		{
			_ = builder.Append(separator).Append(separator).Append("Power: ").Append(info.Creature.Power);
			if(inDuel)
			{
				_ = builder.Append('/').Append(info.Creature.BasePower);
			}
			_ = builder.Append(separator).Append("Life: ").Append(info.Creature.Life);
			if(inDuel)
			{
				_ = builder.Append('/').Append(info.Creature.BaseLife);
			}
			if(info.Location == Location.Field)
			{
				_ = builder.Append(separator).Append("Position: ").Append(info.Creature.Position);
			}
		}
		else if(info.CardTypeCase == CardInfo.CardTypeOneofCase.Spell && !inDuel)
		{
			_ = builder.Append(separator).Append("Can be Class ability: ").Append(info.Spell.CanBeClassAbility);
		}
		return builder.Append(separator).Append("--------").Append(separator).Append(info.Text).ToString();
	}

	public static string? DeckToString(Deck deck)
	{
		if(deck.Name is null)
		{
			return null;
		}
		StringBuilder builder = new();
		_ = builder.Append(deck.PlayerClass);
		if(deck.Ability is not null)
		{
			_ = builder.Append("\n#").Append(deck.Ability.Name);
		}
		if(deck.Quest is not null)
		{
			_ = builder.Append("\n|").Append(deck.Quest.Name);
		}
		foreach(var card in deck.Cards)
		{
			_ = builder.Append('\n').Append(card.Name);
		}
		return builder.ToString();
	}
}

