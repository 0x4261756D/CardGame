using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Google.FlatBuffers;
using CardGameUtils.Constants;
using CardGameUtils.Shared;
using System.Text;

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

	public static CardGameUtils.Packets.Deck.ServerPacket ReadSizedDeckServerPacketFromStream(Stream stream)
	{
		byte[] sizeBuff = new byte[sizeof(int)];
		stream.ReadExactly(sizeBuff);
		int size = BitConverter.ToInt32(sizeBuff);
		Console.WriteLine(size);
		byte[] bytes = new byte[size];
		stream.ReadExactly(bytes);
		ByteBuffer buffer = new(bytes);
		Verifier verifier = new(buffer);
		if(!verifier.VerifyBuffer("deck", false, CardGameUtils.Packets.Deck.ServerPacketVerify.Verify))
		{
			throw new Exception("Could not read packet");
		}
		return CardGameUtils.Packets.Deck.ServerPacket.GetRootAsServerPacket(buffer);
	}
	public static CardGameUtils.Packets.Deck.ClientPacket ReadSizedDeckClientPacketFromStream(Stream stream)
	{
		byte[] sizeBuff = new byte[sizeof(int)];
		stream.ReadExactly(sizeBuff);
		int size = BitConverter.ToInt32(sizeBuff);
		Console.WriteLine(size);
		byte[] bytes = new byte[size];
		stream.ReadExactly(bytes);
		ByteBuffer buffer = new(bytes);
		Verifier verifier = new(buffer);
		if(!CardGameUtils.Packets.Deck.ClientPacket.VerifyClientPacket(buffer))
		{
			throw new Exception("Could not read packet");
		}
		return CardGameUtils.Packets.Deck.ClientPacket.GetRootAsClientPacket(buffer);
	}
	public static CardGameUtils.Packets.Duel.ServerPacket ReadSizedDuelServerPacketFromStream(Stream stream)
	{
		byte[] sizeBuff = new byte[sizeof(int)];
		stream.ReadExactly(sizeBuff);
		int size = BitConverter.ToInt32(sizeBuff);
		Console.WriteLine(size);
		byte[] bytes = new byte[size];
		stream.ReadExactly(bytes);
		ByteBuffer buffer = new(bytes);
		Verifier verifier = new(buffer);
		if(!CardGameUtils.Packets.Duel.ServerPacket.VerifyServerPacket(buffer))
		{
			throw new Exception("Could not read packet");
		}
		return CardGameUtils.Packets.Duel.ServerPacket.GetRootAsServerPacket(buffer);
	}
	public static CardGameUtils.Packets.Duel.ClientPacket ReadSizedDuelClientPacketFromStream(Stream stream)
	{
		byte[] sizeBuff = new byte[sizeof(int)];
		stream.ReadExactly(sizeBuff);
		int size = BitConverter.ToInt32(sizeBuff);
		Console.WriteLine(size);
		byte[] bytes = new byte[size];
		stream.ReadExactly(bytes);
		ByteBuffer buffer = new(bytes);
		if(!CardGameUtils.Packets.Duel.ClientPacket.VerifyClientPacket(buffer))
		{
			throw new Exception("Could not read packet");
		}
		return CardGameUtils.Packets.Duel.ClientPacket.GetRootAsClientPacket(buffer);
	}
	public static CardGameUtils.Packets.Server.ServerPacket ReadSizedServerServerPacketFromStream(Stream stream)
	{
		byte[] sizeBuff = new byte[sizeof(int)];
		stream.ReadExactly(sizeBuff);
		int size = BitConverter.ToInt32(sizeBuff);
		Console.WriteLine(size);
		byte[] bytes = new byte[size];
		stream.ReadExactly(bytes);
		ByteBuffer buffer = new(bytes);
		Verifier verifier = new(buffer);
		if(!CardGameUtils.Packets.Server.ServerPacket.VerifyServerPacket(buffer))
		{
			throw new Exception("Could not read packet");
		}
		return CardGameUtils.Packets.Server.ServerPacket.GetRootAsServerPacket(buffer);
	}
	public static CardGameUtils.Packets.Server.ClientPacket ReadSizedServerClientPacketFromStream(Stream stream)
	{
		byte[] sizeBuff = new byte[sizeof(int)];
		stream.ReadExactly(sizeBuff);
		int size = BitConverter.ToInt32(sizeBuff);
		Console.WriteLine(size);
		byte[] bytes = new byte[size];
		stream.ReadExactly(bytes);
		ByteBuffer buffer = new(bytes);
		Verifier verifier = new(buffer);
		if(!CardGameUtils.Packets.Server.ClientPacket.VerifyClientPacket(buffer))
		{
			throw new Exception("Could not read packet");
		}
		return CardGameUtils.Packets.Server.ClientPacket.GetRootAsClientPacket(buffer);
	}

		public static int TypeSpecificsCreatureOrSpellCost(TypeSpecificsUnion t) => t.Type switch
	{
		TypeSpecifics.creature => t.Ascreature().Cost,
		TypeSpecifics.spell => t.Asspell().Cost,
		_ => throw new Exception($"Tried to get cost of card of type {t.Type}")
	};
	public static int TypeSpecificsCreatureOrSpellBaseCost(TypeSpecificsUnion t) => t.Type switch
	{
		TypeSpecifics.creature => t.Ascreature().BaseCost,
		TypeSpecifics.spell => t.Asspell().BaseCost,
		_ => throw new Exception($"Tried to get base cost of card of type {t.Type}"),
	};
	public static string CardInfoTToString(CardInfoT card, bool inDeckEdit = false, char separator = '\n')
	{
		StringBuilder builder = new();
		if(!inDeckEdit)
		{
			_ = builder.Append("UID: ").Append(card.Uid).Append(separator);
		}
		_ = builder.Append("name: ").Append(card.Name).Append(separator).Append(separator);
		if(card.TypeSpecifics.Type == TypeSpecifics.quest)
		{
			_ = builder.Append("quest progress: ").Append(card.TypeSpecifics.Asquest().Progress).Append('/').Append(card.TypeSpecifics.Asquest().Goal);
		}
		else if(card.Location == Location.Ability)
		{
			_ = builder.Append("cost: 1");
		}
		else
		{
			_ = builder.Append("cost: ").Append(TypeSpecificsCreatureOrSpellCost(card.TypeSpecifics)).Append('/').Append(TypeSpecificsCreatureOrSpellBaseCost(card.TypeSpecifics));
		}
		if(!inDeckEdit)
		{
			_ = builder.Append(separator).Append("controller: ").Append(card.Controller).Append('/').Append(card.BaseController)
				.Append(separator).Append("location: ").Append(card.Location);
		}
		_ = builder.Append(separator).Append("card type: ").Append(card.TypeSpecifics.Type);
		if(card.TypeSpecifics.Type == TypeSpecifics.creature)
		{
			CreatureSpecificsT creatureSpecifics = card.TypeSpecifics.Ascreature();
			_ = builder.Append(separator).Append(separator).Append("power: ").Append(creatureSpecifics.Power);
			if(!inDeckEdit)
			{
				_ = builder.Append('/').Append(creatureSpecifics.BasePower);
			}
			_ = builder.Append(separator).Append("life: ").Append(creatureSpecifics.Life);
			if(!inDeckEdit)
			{
				_ = builder.Append('/').Append(creatureSpecifics.BaseLife);
			}
			if(card.Location == Location.Field)
			{
				_ = builder.Append(separator).Append("position: ").Append(creatureSpecifics.Position);
			}
		}
		else if(inDeckEdit && card.TypeSpecifics.Type == TypeSpecifics.spell)
		{
			_ = builder.Append(separator).Append("can be class ability: ").Append(card.TypeSpecifics.Asspell().CanBeClassAbility);
		}
		return builder.Append(separator).Append("---------").Append(separator).Append(card.Text).ToString();
	}

	public static string? DeckInfoTToString(CardGameUtils.Shared.DeckInfoT deck)
	{
		if(deck.Name is null)
		{
			return null;
		}
		StringBuilder builder = new();
		_ = builder.Append(deck.PlayerClass);
		if(deck.Ability is not null)
		{
			_ = builder.AppendLine().Append('#').Append(deck.Ability.Name);
		}
		if(deck.Quest is not null)
		{
			_ = builder.AppendLine().Append('|').Append(deck.Quest.Name);
		}
		foreach(CardInfoT card in deck.Cards)
		{
			_ = builder.AppendLine().Append(card.Name);
		}
		return builder.AppendLine().ToString();
	}

	public static string ArtworkToFiletypeExtension(CardGameUtils.Packets.Server.ArtworkFiletype filetype) => filetype switch
	{
		CardGameUtils.Packets.Server.ArtworkFiletype.JPG => ".jpg",
		CardGameUtils.Packets.Server.ArtworkFiletype.PNG => ".png",
		_ => throw new NotImplementedException(),
	};

	public static bool IsInLocation(Location first, Location second)
	{
		return first == second || first == Location.Any || second == Location.Any;
	}
}

