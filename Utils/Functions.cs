using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CardGameUtils.Constants;
using CardGameUtils.Packets.Deck;
using CardGameUtils.Packets.Server;
using Thrift;
using Thrift.Transport;

namespace CardGameUtils;

partial class Functions
{
	public class TSimpleFileTransport : TEndpointTransport
	{
		private string path;
		private FileStream? fileStream;
		private OpenMode mode;
		public enum OpenMode
		{
			Read,
			Write
		}

		public TSimpleFileTransport(TConfiguration config, string path, OpenMode mode) : base(config: config)
		{
			if(string.IsNullOrWhiteSpace(path))
			{
				throw new TTransportException("Empty path specified");
			}
			this.path = path;
			this.mode = mode;
			_ = OpenAsync();
		}

		public TSimpleFileTransport(string path, OpenMode mode) : this(new(), path, mode)
		{
		}

		public override Task OpenAsync(CancellationToken cancellationToken = default)
		{
			if(fileStream is null)
			{
				if(mode == OpenMode.Read)
				{
					fileStream = File.OpenRead(path);
				}
				else
				{
					fileStream = File.OpenWrite(path);
				}
			}
			return Task.CompletedTask;
		}
		public override bool IsOpen => fileStream is not null;

		public override void Close()
		{
			if(fileStream is not null)
			{
				fileStream.Close();
				fileStream = null;
			}
		}

		public override async ValueTask<int> ReadAllAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken)
		{
			if(!IsOpen)
			{
				throw new TTransportException("Read operation on a non-open file");
			}
			if(mode != OpenMode.Read)
			{
				throw new TTransportException("Read operation on write only file");
			}
			CheckReadBytesAvailable(length);
			buffer = await File.ReadAllBytesAsync(path, cancellationToken);
			return buffer.Length;
		}

		public override async ValueTask<int> ReadAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken)
		{
			if(!IsOpen)
			{
				throw new TTransportException("Read operation on a non-open file");
			}
			if(mode != OpenMode.Read)
			{
				throw new TTransportException("Read operation on write only file");
			}
			CheckReadBytesAvailable(length);
			int bytesRead = 0;
			try
			{
				bytesRead = await fileStream!.ReadAsync(buffer.AsMemory(offset, length));
			}
			catch(Exception e)
			{
				fileStream = null;
				throw new TTransportException(e.Message);
			}
			return bytesRead;
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken = default)
		{
			if(!IsOpen)
			{
				throw new TTransportException("Write operation on a non-open file");
			}
			if(mode != OpenMode.Write)
			{
				throw new TTransportException("Write operation on a write only file");
			}
			try
			{
				await fileStream!.WriteAsync(buffer.AsMemory(offset, length), cancellationToken: cancellationToken);
			}
			catch(Exception e)
			{
				throw new TTransportException(e.Message);
			}
		}
		public override async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
		{
			if(!IsOpen)
			{
				throw new TTransportException("Write operation on a non-open file");
			}
			if(mode != OpenMode.Write)
			{
				throw new TTransportException("Write operation on a write only file");
			}
			try
			{
				await fileStream!.WriteAsync(buffer, cancellationToken: cancellationToken);
			}
			catch(Exception e)
			{
				throw new TTransportException(e.Message);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(fileStream is not null && disposing)
			{
				fileStream.Dispose();
				fileStream = null;
			}
		}

		public override async Task FlushAsync(CancellationToken cancellationToken)
		{
			if(!IsOpen)
			{
				throw new TTransportException("Flush operation on a non-open file");
			}
			await fileStream!.FlushAsync(cancellationToken: cancellationToken);
		}
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

	public static int? CardInfoCost(CardInfo info) => info.Card_type_specifics?.As_creature?.Cost ?? info.Card_type_specifics?.As_spell?.Cost;
	public static int? CardInfoBaseCost(CardInfo info) => info.Card_type_specifics?.As_creature?.Base_cost ?? info.Card_type_specifics?.As_spell?.Base_cost;

	public static string FormatCardInfo(CardInfo info, bool inDeckEdit = false, char separator = '\n')
	{
		StringBuilder builder = new();
		if(!inDeckEdit)
		{
			_ = builder.Append("UID: ").Append(info.Uid).Append(separator);
		}
		_ = builder.Append("name: ").Append(info.Name).Append(separator).Append(separator);
		if(info.Card_type_specifics is CardTypeSpecifics.quest)
		{
			_ = builder.Append("quest progress: ").Append(info.Card_type_specifics.As_quest!.Progress).Append('/').Append(info.Card_type_specifics.As_quest!.Goal);
		}
		else if(info.Location == Location.Ability)
		{
			_ = builder.Append("Cost: 1");
		}
		else
		{
			_ = builder.Append("Cost: ").Append(CardInfoCost(info));
			if(!inDeckEdit)
			{
				_ = builder.Append('/').Append(CardInfoBaseCost(info));
			}
		}
		if(!inDeckEdit)
		{
			_ = builder.Append(separator).Append("controller: ").Append(info.Controller).Append('/').Append(info.Base_controller);
		}
		_ = builder.Append(separator).Append(info.Card_type_specifics).Append(separator).Append("class: ").Append(info.Card_class);
		if(!inDeckEdit)
		{
			_ = builder.Append(separator).Append("Location").Append(info.Location);
		}
		if(info.Card_type_specifics is CardTypeSpecifics.creature)
		{
			_ = builder.Append(separator).Append("power: ").Append(info.Card_type_specifics.As_creature!.Power);
			if(!inDeckEdit)
			{
				_ = builder.Append('/').Append(info.Card_type_specifics.As_creature!.Base_power);
			}
			_ = builder.Append(separator).Append("life: ").Append(info.Card_type_specifics.As_creature!.Life);
			if(!inDeckEdit)
			{
				_ = builder.Append('/').Append(info.Card_type_specifics.As_creature!.Base_life);
			}
			if(info.Location == Location.Field)
			{
				_ = builder.Append(separator).Append("position: ").Append(info.Card_type_specifics.As_creature!.Position);
			}
		}
		else if(inDeckEdit && info.Card_type_specifics is CardTypeSpecifics.spell)
		{
			_ = builder.Append(separator).Append("can be class ability: ").Append(info.Card_type_specifics.As_spell!.Can_be_class_ability);
		}
		return builder.Append(separator).Append("----------").Append(separator).Append(info.Text).ToString();
	}

	public static string? DeckInfoToString(DeckInfo deck)
	{
		if(deck.Name is null)
		{
			return null;
		}
		StringBuilder builder = new();
		_ = builder.Append(deck.Player_class);
		if(deck.Ability is not null)
		{
			_ = builder.AppendLine().Append('#').Append(deck.Ability.Name);
		}
		if(deck.Quest is not null)
		{
			_ = builder.AppendLine().Append('|').Append(deck.Quest.Name);
		}
		if(deck.Cards is not null)
		{
			foreach(CardInfo card in deck.Cards)
			{
				_ = builder.AppendLine().Append(card.Name);
			}
		}
		return builder.AppendLine().ToString();
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

	public static string ArtworkFiletypeToExtension(ArtworkFiletype filetype)
	{
		return filetype switch
		{
			ArtworkFiletype.JPG => ".jpg",
			ArtworkFiletype.PNG => ".png",
			_ => throw new NotImplementedException(),
		};
	}

	public static bool IsInLocation(Location first, Location second)
	{
		return first == second || first == Location.Any || second == Location.Any;
	}
}

