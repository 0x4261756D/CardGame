using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Structs.Duel;

#nullable enable

public record SToC_Packet(SToC_Content content) : Common.PacketTable
{
	public byte[] Deserialize()
	{
		List<byte> dataBytes = DeserializeInternal();
		return [.. Common.Common.DeserializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.DeserializeN16(2) /* ProtoVersion */,
			.. Common.Common.DeserializeN16(1) /* SchemaVersion */,
			.. Common.Common.DeserializeName("SToC_Packet") /* Name */,
			.. dataBytes /* Root */];
	}

	public static SToC_Packet Serialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.SerializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return SerializeImpl(ref bytes);
	}
	public static SToC_Packet Serialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.SerializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return SerializeImpl(ref bytes);
	}
	private static SToC_Packet SerializeImpl(ref Span<byte> bytes)
	{
		ushort protoVersion = Common.Common.SerializeN16(ref bytes);
		if(protoVersion != 2)
		{
			throw new Exception($"Wrong proto version, expected 2, got {protoVersion}");
		}
		ushort schemaVersion = Common.Common.SerializeN16(ref bytes);
		if(schemaVersion != 1)
		{
			throw new Exception($"Wrong schema version, expected 1, got {schemaVersion}");
		}
		if(!Common.Common.SerializeName(ref bytes, "SToC_Packet"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		SToC_Packet ret = SerializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static SToC_Packet SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header content */
		{
			if(!Common.Common.SerializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header SToC_Packet.content hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for SToC_Packet.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		SToC_Content content = SToC_Content.SerializeInternal(ref bytes);
		return new(content);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header content */
		bytes.AddRange(Common.Common.DeserializeName("content")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data content */
		bytes.AddRange(content.DeserializeInternal());
		return bytes;
	}
}
public interface SToC_Content : Common.PacketUnion
{
	public static SToC_Content SerializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.DeserializeName("game_result")))
		{
			return game_result.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("get_actions")))
		{
			return get_actions.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("yes_no")))
		{
			return yes_no.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("select_cards")))
		{
			return select_cards.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("select_cards_custom")))
		{
			return select_cards_custom.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("select_cards_custom_intermediate")))
		{
			return select_cards_custom_intermediate.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("select_zone")))
		{
			return select_zone.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("field_update")))
		{
			return field_update.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("show_cards")))
		{
			return show_cards.SerializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in SToC_Content");
		}
	}

	public record game_result(SToC_Broadcast_GameResult value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("game_result")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static game_result SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/game_result, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Broadcast_GameResult value = SToC_Broadcast_GameResult.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record get_actions(SToC_Response_GetActions value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("get_actions")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static get_actions SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/get_actions, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_GetActions value = SToC_Response_GetActions.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record yes_no(SToC_Request_YesNo value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("yes_no")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static yes_no SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/yes_no, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Request_YesNo value = SToC_Request_YesNo.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record select_cards(SToC_Request_SelectCards value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("select_cards")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static select_cards SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/select_cards, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Request_SelectCards value = SToC_Request_SelectCards.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record select_cards_custom(SToC_Request_SelectCardsCustom value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("select_cards_custom")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static select_cards_custom SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/select_cards_custom, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Request_SelectCardsCustom value = SToC_Request_SelectCardsCustom.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record select_cards_custom_intermediate(SToC_Response_SelectCardsCustomIntermediate value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("select_cards_custom_intermediate")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static select_cards_custom_intermediate SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/select_cards_custom_intermediate, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_SelectCardsCustomIntermediate value = SToC_Response_SelectCardsCustomIntermediate.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record select_zone(SToC_Request_SelectZone value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("select_zone")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static select_zone SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/select_zone, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Request_SelectZone value = SToC_Request_SelectZone.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record field_update(SToC_Broadcast_FieldUpdate value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("field_update")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static field_update SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/field_update, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Broadcast_FieldUpdate value = SToC_Broadcast_FieldUpdate.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record show_cards(SToC_Response_ShowCards value) : SToC_Content
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("show_cards")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static show_cards SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Content/show_cards, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SToC_Response_ShowCards value = SToC_Response_ShowCards.SerializeInternal(ref bytes);
			return new(value);
		}
	}
}
public record SToC_Broadcast_GameResult(CardGameUtils.GameConstants.GameResult result) : Common.PacketTable
{
	public static SToC_Broadcast_GameResult SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header result */
		{
			if(!Common.Common.SerializeName(ref bytes, "result")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_GameResult.result hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_GameResult.result, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		CardGameUtils.GameConstants.GameResult result = (CardGameUtils.GameConstants.GameResult)Common.Common.SerializeN8(ref bytes);
		if(!Common.Common.SerializeName(ref bytes, Enum.GetName(result)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.DeserializeName(Enum.GetName(result)!))}]");
		}
		return new(result);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header result */
		bytes.AddRange(Common.Common.DeserializeName("result")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Data result */
		bytes.AddRange(Common.Common.DeserializeN8((byte)result));
		bytes.AddRange(Common.Common.DeserializeName(Enum.GetName(result)!, len: 3));
		return bytes;
	}
}
public record SToC_Response_GetActions(CardGameUtils.GameConstants.Location location, uint uid, List<CardGameUtils.Base.CardAction> actions) : Common.PacketTable
{
	public static SToC_Response_GetActions SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header location */
		{
			if(!Common.Common.SerializeName(ref bytes, "location")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_GetActions.location hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for SToC_Response_GetActions.location, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header uid */
		{
			if(!Common.Common.SerializeName(ref bytes, "uid")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_GetActions.uid hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for SToC_Response_GetActions.uid, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header actions */
		{
			if(!Common.Common.SerializeName(ref bytes, "actions")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_GetActions.actions hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_GetActions.actions, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		CardGameUtils.GameConstants.Location location = (CardGameUtils.GameConstants.Location)Common.Common.SerializeN8(ref bytes);
		if(!Common.Common.SerializeName(ref bytes, Enum.GetName(location)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.DeserializeName(Enum.GetName(location)!))}]");
		}
		uint uid = Common.Common.SerializeN32(ref bytes);
		byte actionsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(actionsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for actions, expected 0, got {actionsNestingLevel}");
		}
		uint actionsCount = Common.Common.SerializeN32(ref bytes);
		List<CardGameUtils.Base.CardAction> actions = new((int)actionsCount);
		for(int actions_ = 0; actions_ < actions.Capacity; actions_++)
		{
			actions.Add(CardGameUtils.Base.CardAction.SerializeInternal(ref bytes));
		}
		return new(location, uid, actions);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header location */
		bytes.AddRange(Common.Common.DeserializeName("location")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header uid */
		bytes.AddRange(Common.Common.DeserializeName("uid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header actions */
		bytes.AddRange(Common.Common.DeserializeName("actions")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data location */
		bytes.AddRange(Common.Common.DeserializeN8((byte)location));
		bytes.AddRange(Common.Common.DeserializeName(Enum.GetName(location)!, len: 3));
		/* Data uid */
		bytes.AddRange(Common.Common.DeserializeN32(uid));
		/* Data actions */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)actions.Count)); /* Count */
		/* Nesting Counts */
		foreach(var actions_ in actions)
		{
			bytes.AddRange(actions_.DeserializeInternal());
		}
		return bytes;
	}
}
public record SToC_Request_YesNo(string question) : Common.PacketTable
{
	public static SToC_Request_YesNo SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header question */
		{
			if(!Common.Common.SerializeName(ref bytes, "question")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_YesNo.question hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for SToC_Request_YesNo.question, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		string question = Common.Common.SerializeStr(ref bytes);
		return new(question);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header question */
		bytes.AddRange(Common.Common.DeserializeName("question")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data question */
		bytes.AddRange(Common.Common.DeserializeStr(question));
		return bytes;
	}
}
public record SToC_Request_SelectCards(List<CardGameUtils.Base.CardStruct> cards, string description, uint amount) : Common.PacketTable
{
	public static SToC_Request_SelectCards SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header cards */
		{
			if(!Common.Common.SerializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_SelectCards.cards hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Request_SelectCards.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header description */
		{
			if(!Common.Common.SerializeName(ref bytes, "description")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_SelectCards.description hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for SToC_Request_SelectCards.description, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header amount */
		{
			if(!Common.Common.SerializeName(ref bytes, "amount")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_SelectCards.amount hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for SToC_Request_SelectCards.amount, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		byte cardsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(cardsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cards, expected 0, got {cardsNestingLevel}");
		}
		uint cardsCount = Common.Common.SerializeN32(ref bytes);
		List<CardGameUtils.Base.CardStruct> cards = new((int)cardsCount);
		for(int cards_ = 0; cards_ < cards.Capacity; cards_++)
		{
			cards.Add(CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes));
		}
		string description = Common.Common.SerializeStr(ref bytes);
		uint amount = Common.Common.SerializeN32(ref bytes);
		return new(cards, description, amount);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header cards */
		bytes.AddRange(Common.Common.DeserializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header description */
		bytes.AddRange(Common.Common.DeserializeName("description")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header amount */
		bytes.AddRange(Common.Common.DeserializeName("amount")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Data cards */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)cards.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cards_ in cards)
		{
			bytes.AddRange(cards_.DeserializeInternal());
		}
		/* Data description */
		bytes.AddRange(Common.Common.DeserializeStr(description));
		/* Data amount */
		bytes.AddRange(Common.Common.DeserializeN32(amount));
		return bytes;
	}
}
public record SToC_Request_SelectCardsCustom(List<CardGameUtils.Base.CardStruct> cards, string description, bool initial_state) : Common.PacketTable
{
	public static SToC_Request_SelectCardsCustom SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header cards */
		{
			if(!Common.Common.SerializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_SelectCardsCustom.cards hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Request_SelectCardsCustom.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header description */
		{
			if(!Common.Common.SerializeName(ref bytes, "description")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_SelectCardsCustom.description hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for SToC_Request_SelectCardsCustom.description, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header initial_state */
		{
			if(!Common.Common.SerializeName(ref bytes, "initial_state")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_SelectCardsCustom.initial_state hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SToC_Request_SelectCardsCustom.initial_state, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		byte cardsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(cardsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cards, expected 0, got {cardsNestingLevel}");
		}
		uint cardsCount = Common.Common.SerializeN32(ref bytes);
		List<CardGameUtils.Base.CardStruct> cards = new((int)cardsCount);
		for(int cards_ = 0; cards_ < cards.Capacity; cards_++)
		{
			cards.Add(CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes));
		}
		string description = Common.Common.SerializeStr(ref bytes);
		bool initial_state = Common.Common.SerializeBool(ref bytes);
		return new(cards, description, initial_state);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header cards */
		bytes.AddRange(Common.Common.DeserializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header description */
		bytes.AddRange(Common.Common.DeserializeName("description")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header initial_state */
		bytes.AddRange(Common.Common.DeserializeName("initial_state")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data cards */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)cards.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cards_ in cards)
		{
			bytes.AddRange(cards_.DeserializeInternal());
		}
		/* Data description */
		bytes.AddRange(Common.Common.DeserializeStr(description));
		/* Data initial_state */
		bytes.AddRange(Common.Common.DeserializeBool(initial_state));
		return bytes;
	}
}
public record SToC_Response_SelectCardsCustomIntermediate(bool is_valid) : Common.PacketTable
{
	public static SToC_Response_SelectCardsCustomIntermediate SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header is_valid */
		{
			if(!Common.Common.SerializeName(ref bytes, "is_valid")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_SelectCardsCustomIntermediate.is_valid hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SToC_Response_SelectCardsCustomIntermediate.is_valid, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		bool is_valid = Common.Common.SerializeBool(ref bytes);
		return new(is_valid);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header is_valid */
		bytes.AddRange(Common.Common.DeserializeName("is_valid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data is_valid */
		bytes.AddRange(Common.Common.DeserializeBool(is_valid));
		return bytes;
	}
}
public record SToC_Request_SelectZone(List<bool> options) : Common.PacketTable
{
	public static SToC_Request_SelectZone SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header options */
		{
			if(!Common.Common.SerializeName(ref bytes, "options")) /* Name */
			{
				throw new Exception("Field Header SToC_Request_SelectZone.options hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Request_SelectZone.options, expected {(byte)(Common.TypeBytes.Bool | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte optionsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(optionsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for options, expected 0, got {optionsNestingLevel}");
		}
		uint optionsCount = Common.Common.SerializeN32(ref bytes);
		List<bool> options = new((int)optionsCount);
		for(int options_ = 0; options_ < options.Capacity; options_++)
		{
			options.Add(Common.Common.SerializeBool(ref bytes));
		}
		return new(options);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header options */
		bytes.AddRange(Common.Common.DeserializeName("options")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool | Common.TypeBytes.ListFlag)); /* Type */
		/* Data options */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)options.Count)); /* Count */
		/* Nesting Counts */
		foreach(var options_ in options)
		{
			bytes.AddRange(Common.Common.DeserializeBool(options_));
		}
		return bytes;
	}
}
public record SToC_Broadcast_FieldUpdate(FieldStruct own_field, FieldStruct opp_field, ShownInfo? own_shown_info, ShownInfo? opp_shown_info, uint turn, bool has_initiative, bool is_battle_direction_left_to_right, int? marked_zone) : Common.PacketTable
{
	public static SToC_Broadcast_FieldUpdate SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header own_field */
		{
			if(!Common.Common.SerializeName(ref bytes, "own_field")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.own_field hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.own_field, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		/* Field Header opp_field */
		{
			if(!Common.Common.SerializeName(ref bytes, "opp_field")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.opp_field hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.opp_field, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		/* Field Header own_shown_info */
		{
			if(!Common.Common.SerializeName(ref bytes, "own_shown_info")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.own_shown_info hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.own_shown_info, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		/* Field Header opp_shown_info */
		{
			if(!Common.Common.SerializeName(ref bytes, "opp_shown_info")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.opp_shown_info hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.opp_shown_info, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		/* Field Header turn */
		{
			if(!Common.Common.SerializeName(ref bytes, "turn")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.turn hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.turn, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header has_initiative */
		{
			if(!Common.Common.SerializeName(ref bytes, "has_initiative")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.has_initiative hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.has_initiative, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		/* Field Header is_battle_direction_left_to_right */
		{
			if(!Common.Common.SerializeName(ref bytes, "is_battle_direction_left_to_right")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.is_battle_direction_left_to_right hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.is_battle_direction_left_to_right, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		/* Field Header marked_zone */
		{
			if(!Common.Common.SerializeName(ref bytes, "marked_zone")) /* Name */
			{
				throw new Exception("Field Header SToC_Broadcast_FieldUpdate.marked_zone hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32 | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for SToC_Broadcast_FieldUpdate.marked_zone, expected {(byte)(Common.TypeBytes.I32 | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		FieldStruct own_field = FieldStruct.SerializeInternal(ref bytes);
		FieldStruct opp_field = FieldStruct.SerializeInternal(ref bytes);
		ShownInfo? own_shown_info = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			own_shown_info = ShownInfo.SerializeInternal(ref bytes);
		}
		ShownInfo? opp_shown_info = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			opp_shown_info = ShownInfo.SerializeInternal(ref bytes);
		}
		uint turn = Common.Common.SerializeN32(ref bytes);
		bool has_initiative = Common.Common.SerializeBool(ref bytes);
		bool is_battle_direction_left_to_right = Common.Common.SerializeBool(ref bytes);
		int? marked_zone = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			marked_zone = Common.Common.SerializeI32(ref bytes);
		}
		return new(own_field, opp_field, own_shown_info, opp_shown_info, turn, has_initiative, is_battle_direction_left_to_right, marked_zone);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header own_field */
		bytes.AddRange(Common.Common.DeserializeName("own_field")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Field Header opp_field */
		bytes.AddRange(Common.Common.DeserializeName("opp_field")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Field Header own_shown_info */
		bytes.AddRange(Common.Common.DeserializeName("own_shown_info")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Field Header opp_shown_info */
		bytes.AddRange(Common.Common.DeserializeName("opp_shown_info")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Field Header turn */
		bytes.AddRange(Common.Common.DeserializeName("turn")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header has_initiative */
		bytes.AddRange(Common.Common.DeserializeName("has_initiative")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Field Header is_battle_direction_left_to_right */
		bytes.AddRange(Common.Common.DeserializeName("is_battle_direction_left_to_right")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Field Header marked_zone */
		bytes.AddRange(Common.Common.DeserializeName("marked_zone")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32 | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data own_field */
		bytes.AddRange(own_field.DeserializeInternal());
		/* Data opp_field */
		bytes.AddRange(opp_field.DeserializeInternal());
		/* Data own_shown_info */
		if(own_shown_info is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(own_shown_info.DeserializeInternal());
		}
		/* Data opp_shown_info */
		if(opp_shown_info is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(opp_shown_info.DeserializeInternal());
		}
		/* Data turn */
		bytes.AddRange(Common.Common.DeserializeN32(turn));
		/* Data has_initiative */
		bytes.AddRange(Common.Common.DeserializeBool(has_initiative));
		/* Data is_battle_direction_left_to_right */
		bytes.AddRange(Common.Common.DeserializeBool(is_battle_direction_left_to_right));
		/* Data marked_zone */
		if(marked_zone is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(Common.Common.DeserializeI32(marked_zone.Value));
		}
		return bytes;
	}
}
public record SToC_Response_ShowCards(List<CardGameUtils.Base.CardStruct> cards) : Common.PacketTable
{
	public static SToC_Response_ShowCards SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header cards */
		{
			if(!Common.Common.SerializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header SToC_Response_ShowCards.cards hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for SToC_Response_ShowCards.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte cardsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(cardsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cards, expected 0, got {cardsNestingLevel}");
		}
		uint cardsCount = Common.Common.SerializeN32(ref bytes);
		List<CardGameUtils.Base.CardStruct> cards = new((int)cardsCount);
		for(int cards_ = 0; cards_ < cards.Capacity; cards_++)
		{
			cards.Add(CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes));
		}
		return new(cards);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header cards */
		bytes.AddRange(Common.Common.DeserializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Data cards */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)cards.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cards_ in cards)
		{
			bytes.AddRange(cards_.DeserializeInternal());
		}
		return bytes;
	}
}
public record FieldStruct(int life, uint deck_size, uint grave_size, int momentum, List<CardGameUtils.Base.CardStruct> hand, List<CardGameUtils.Base.CardStruct?> field, string? name, CardGameUtils.Base.CardStruct ability, CardGameUtils.Base.CardStruct quest) : Common.PacketTable
{
	public static FieldStruct SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header life */
		{
			if(!Common.Common.SerializeName(ref bytes, "life")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.life hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for FieldStruct.life, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header deck_size */
		{
			if(!Common.Common.SerializeName(ref bytes, "deck_size")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.deck_size hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for FieldStruct.deck_size, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header grave_size */
		{
			if(!Common.Common.SerializeName(ref bytes, "grave_size")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.grave_size hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for FieldStruct.grave_size, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header momentum */
		{
			if(!Common.Common.SerializeName(ref bytes, "momentum")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.momentum hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for FieldStruct.momentum, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header hand */
		{
			if(!Common.Common.SerializeName(ref bytes, "hand")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.hand hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for FieldStruct.hand, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header field */
		{
			if(!Common.Common.SerializeName(ref bytes, "field")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.field hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for FieldStruct.field, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header name */
		{
			if(!Common.Common.SerializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for FieldStruct.name, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		/* Field Header ability */
		{
			if(!Common.Common.SerializeName(ref bytes, "ability")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.ability hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for FieldStruct.ability, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		/* Field Header quest */
		{
			if(!Common.Common.SerializeName(ref bytes, "quest")) /* Name */
			{
				throw new Exception("Field Header FieldStruct.quest hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for FieldStruct.quest, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		int life = Common.Common.SerializeI32(ref bytes);
		uint deck_size = Common.Common.SerializeN32(ref bytes);
		uint grave_size = Common.Common.SerializeN32(ref bytes);
		int momentum = Common.Common.SerializeI32(ref bytes);
		byte handNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(handNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for hand, expected 0, got {handNestingLevel}");
		}
		uint handCount = Common.Common.SerializeN32(ref bytes);
		List<CardGameUtils.Base.CardStruct> hand = new((int)handCount);
		for(int hand_ = 0; hand_ < hand.Capacity; hand_++)
		{
			hand.Add(CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes));
		}
		byte fieldNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(fieldNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for field, expected 0, got {fieldNestingLevel}");
		}
		uint fieldCount = Common.Common.SerializeN32(ref bytes);
		List<CardGameUtils.Base.CardStruct?> field = new((int)fieldCount);
		for(int field_ = 0; field_ < field.Capacity; field_++)
		{
			field.Add(null);
			if(Common.Common.SerializeBool(ref bytes))
			{
				field[field_] = CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes);
			}
		}
		string? name = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			name = Common.Common.SerializeStr(ref bytes);
		}
		CardGameUtils.Base.CardStruct ability = CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes);
		CardGameUtils.Base.CardStruct quest = CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes);
		return new(life, deck_size, grave_size, momentum, hand, field, name, ability, quest);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header life */
		bytes.AddRange(Common.Common.DeserializeName("life")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header deck_size */
		bytes.AddRange(Common.Common.DeserializeName("deck_size")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header grave_size */
		bytes.AddRange(Common.Common.DeserializeName("grave_size")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header momentum */
		bytes.AddRange(Common.Common.DeserializeName("momentum")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header hand */
		bytes.AddRange(Common.Common.DeserializeName("hand")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header field */
		bytes.AddRange(Common.Common.DeserializeName("field")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header name */
		bytes.AddRange(Common.Common.DeserializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Field Header ability */
		bytes.AddRange(Common.Common.DeserializeName("ability")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Field Header quest */
		bytes.AddRange(Common.Common.DeserializeName("quest")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Data life */
		bytes.AddRange(Common.Common.DeserializeI32(life));
		/* Data deck_size */
		bytes.AddRange(Common.Common.DeserializeN32(deck_size));
		/* Data grave_size */
		bytes.AddRange(Common.Common.DeserializeN32(grave_size));
		/* Data momentum */
		bytes.AddRange(Common.Common.DeserializeI32(momentum));
		/* Data hand */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)hand.Count)); /* Count */
		/* Nesting Counts */
		foreach(var hand_ in hand)
		{
			bytes.AddRange(hand_.DeserializeInternal());
		}
		/* Data field */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)field.Count)); /* Count */
		/* Nesting Counts */
		foreach(var field_ in field)
		{
			if(field_ is null)
			{
				bytes.Add(0); /* IsSet */
			}
			else
			{
				bytes.Add(1); /* IsSet */
				bytes.AddRange(field_.DeserializeInternal());
			}
		}
		/* Data name */
		if(name is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(Common.Common.DeserializeStr(name));
		}
		/* Data ability */
		bytes.AddRange(ability.DeserializeInternal());
		/* Data quest */
		bytes.AddRange(quest.DeserializeInternal());
		return bytes;
	}
}
public record ShownInfo(CardGameUtils.Base.CardStruct? card, string? description) : Common.PacketTable
{
	public static ShownInfo SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header card */
		{
			if(!Common.Common.SerializeName(ref bytes, "card")) /* Name */
			{
				throw new Exception("Field Header ShownInfo.card hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for ShownInfo.card, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		/* Field Header description */
		{
			if(!Common.Common.SerializeName(ref bytes, "description")) /* Name */
			{
				throw new Exception("Field Header ShownInfo.description hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for ShownInfo.description, expected {(byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		CardGameUtils.Base.CardStruct? card = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			card = CardGameUtils.Base.CardStruct.SerializeInternal(ref bytes);
		}
		string? description = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			description = Common.Common.SerializeStr(ref bytes);
		}
		return new(card, description);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header card */
		bytes.AddRange(Common.Common.DeserializeName("card")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Field Header description */
		bytes.AddRange(Common.Common.DeserializeName("description")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data card */
		if(card is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(card.DeserializeInternal());
		}
		/* Data description */
		if(description is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(Common.Common.DeserializeStr(description));
		}
		return bytes;
	}
}
