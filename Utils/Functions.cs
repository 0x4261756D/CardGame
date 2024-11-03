using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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

	public static string ArtworkFiletypeToExtension(Structs.Server.ArtworkFiletype filetype)
	{
		return filetype switch
		{
			Structs.Server.ArtworkFiletype.JPG => ".jpg",
			Structs.Server.ArtworkFiletype.PNG => ".png",
			_ => throw new NotImplementedException(),
		};
	}

	public static bool IsInLocation(GameConstants.Location first, GameConstants.Location second)
	{
		return first == second || first == GameConstants.Location.Any || second == GameConstants.Location.Any;
	}
}

