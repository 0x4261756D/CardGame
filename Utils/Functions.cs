using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CardGameUtils.Base;
using CardGameUtils.GameEnumsAndStructs;

namespace CardGameUtils;

partial class Functions
{
	public static string GetDeckString(Deck deck)
	{
		StringBuilder builder = new();
		_ = builder.Append(deck.player_class);
		if(deck.ability is not null)
		{
			_ = builder.AppendLine().Append('#').Append(deck.ability.name);
		}
		if(deck.quest is not null)
		{
			_ = builder.AppendLine().Append('|').Append(deck.quest.name);
		}
		foreach(CardStruct card in deck.cards)
		{
			_ = builder.AppendLine().Append(card.name);
		}
		Log(builder.ToString(), LogSeverity.Warning);
		return builder.AppendLine().ToString();
	}

	public static string FormatCardStruct(CardStruct card, char separator = '\n', bool includeInfoIrrelevantForDeckEdit = true)
	{
		StringBuilder builder = new();
		if(includeInfoIrrelevantForDeckEdit)
		{
			_ = builder.Append("UID: ").Append(card.uid).Append(separator);
		}
		_ = builder.Append("name: ").Append(card.name).Append(separator, 2);
		if(card.type_specifics is TypeSpecifics.quest quest)
		{
			_ = builder.Append("quest progress: ").Append(quest.value.progress).Append('/').Append(quest.value.goal);
		}
		else if(card.location == Location.Ability)
		{
			_ = builder.Append("cost: 1");
		}
		else
		{
			_ = builder.Append("cost: ");
			if(card.type_specifics is TypeSpecifics.creature creature)
			{
				_ = builder.Append(creature.value.cost);
				if(includeInfoIrrelevantForDeckEdit)
				{
					_ = builder.Append('/').Append(creature.value.base_cost);
				}
			}
			else if(card.type_specifics is TypeSpecifics.spell spell)
			{
				_ = builder.Append(spell.value.cost);
				if(includeInfoIrrelevantForDeckEdit)
				{
					_ = builder.Append('/').Append(spell.value.base_cost);
				}
			}
		}
		if(includeInfoIrrelevantForDeckEdit)
		{
			_ = builder.Append(separator).Append("controller: ").Append(card.controller).Append('/').Append(card.base_controller);
		}
		_ = builder.Append(separator).Append("class: ").Append(card.card_class);
		if(includeInfoIrrelevantForDeckEdit)
		{
			_ = builder.Append(separator).Append("location: ").Append(card.location);
		}
		{
			if(card.type_specifics is TypeSpecifics.creature creature)
			{
				_ = builder.Append(separator).Append("power: ").Append(creature.value.power);
				if(includeInfoIrrelevantForDeckEdit)
				{
					_ = builder.Append('/').Append(creature.value.base_power);
				}
				_ = builder.Append(separator).Append("life: ").Append(creature.value.life);
				if(includeInfoIrrelevantForDeckEdit)
				{
					_ = builder.Append('/').Append(creature.value.base_life);
				}
				if(card.location == Location.Field)
				{
					_ = builder.Append(separator).Append("position: ").Append(creature.value.position);
				}
			}
			else if(!includeInfoIrrelevantForDeckEdit && card.type_specifics is TypeSpecifics.spell spell)
			{
				_ = builder.Append(separator).Append("can be class ability: ").Append(spell.value.can_be_class_ability).Append(separator).Append("is class ability: ").Append(spell.value.is_class_ability);
			}
		}
		_ = builder.Append(separator).Append('-', 16).Append(separator, 2).Append(card.text).Append(separator);
		return builder.ToString();
	}
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
		Console.WriteLine($"{severity.ToString().ToUpperInvariant()}: [{(includeFullPath ? propertyName : Path.GetFileNameWithoutExtension(propertyName))}:{lineNumber}]: {message}");
#if RELEASE
		}
#endif
		Console.ForegroundColor = current;
	}

	public static string ArtworkFiletypeToExtension(Structs.Server.ArtworkFiletype filetype)
	{
		return filetype switch
		{
			Structs.Server.ArtworkFiletype.JPG => ".jpg",
			Structs.Server.ArtworkFiletype.PNG => ".png",
			_ => throw new NotImplementedException(),
		};
	}

	public static bool IsInLocation(Location first, Location second)
	{
		return first == second || first == Location.Any || second == Location.Any;
	}
}

