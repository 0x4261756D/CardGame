using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CardGameUtils.Structs.Duel;

#nullable enable
#pragma warning disable CS8981
internal record CToS_Packet(CToS_Content content) : Common.PacketTable
{
	public byte[] Serialize()
	{
		List<byte> dataBytes = SerializeInternal();
		return [.. Common.Common.SerializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.SerializeN16(2) /* ProtoVersion */,
			.. Common.Common.SerializeN16(1) /* SchemaVersion */,
			.. Common.Common.SerializeName("CToS_Packet") /* Name */,
			.. dataBytes /* Root */];
	}

	public static CToS_Packet Deserialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.DeserializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return DeserializeImpl(ref bytes);
	}
	public static CToS_Packet Deserialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return DeserializeImpl(ref bytes);
	}
	public static async Task<CToS_Packet> DeserializeAsync(Stream stream, CancellationToken token)
	{
		byte[] sizeBytes = new byte[4];
		await stream.ReadExactlyAsync(sizeBytes.AsMemory(), token);
		Span<byte> sizeSpan = sizeBytes.AsSpan();
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		byte[] bytes = new byte[size];
		await stream.ReadExactlyAsync(bytes.AsMemory(), token);
		Span<byte> byteSpan = bytes.AsSpan();
		return DeserializeImpl(ref byteSpan);
	}
	private static CToS_Packet DeserializeImpl(ref Span<byte> bytes)
	{
		ushort protoVersion = Common.Common.DeserializeN16(ref bytes);
		if(protoVersion != 2)
		{
			throw new Exception($"Wrong proto version, expected 2, got {protoVersion}");
		}
		ushort schemaVersion = Common.Common.DeserializeN16(ref bytes);
		if(schemaVersion != 1)
		{
			throw new Exception($"Wrong schema version, expected 1, got {schemaVersion}");
		}
		if(!Common.Common.DeserializeName(ref bytes, "CToS_Packet"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		CToS_Packet ret = DeserializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static CToS_Packet DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header content */
		{
			if(!Common.Common.DeserializeName(ref bytes, "content")) /* Name */
			{
				throw new Exception("Field Header CToS_Packet.content hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for CToS_Packet.content, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		CToS_Content content = CToS_Content.DeserializeInternal(ref bytes);
		return new(content);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header content */
		bytes.AddRange(Common.Common.SerializeName("content")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data content */
		bytes.AddRange(content.SerializeInternal());
		return bytes;
	}
}
internal interface CToS_Content : Common.PacketUnion
{
	public static CToS_Content DeserializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.SerializeName("surrender")))
		{
			return surrender.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("get_actions")))
		{
			return get_actions.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("select_option")))
		{
			return select_option.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("yes_no")))
		{
			return yes_no.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("select_cards")))
		{
			return select_cards.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("select_cards_custom_intermediate")))
		{
			return select_cards_custom_intermediate.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("select_cards_custom")))
		{
			return select_cards_custom.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("select_zone")))
		{
			return select_zone.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("pass")))
		{
			return pass.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("view_grave")))
		{
			return view_grave.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in CToS_Content");
		}
	}

	internal record surrender() : CToS_Content
	{
		public static surrender DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/surrender, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("surrender"), (byte)Common.TypeBytes.Void];
		}
	}
	internal record get_actions(CToS_Request_GetActions value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("get_actions")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static get_actions DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/get_actions, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_GetActions value = CToS_Request_GetActions.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record select_option(CToS_Request_SelectOption value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("select_option")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static select_option DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/select_option, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_SelectOption value = CToS_Request_SelectOption.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record yes_no(CToS_Response_YesNo value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("yes_no")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static yes_no DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/yes_no, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Response_YesNo value = CToS_Response_YesNo.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record select_cards(CToS_Response_SelectCards value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("select_cards")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static select_cards DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/select_cards, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Response_SelectCards value = CToS_Response_SelectCards.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record select_cards_custom_intermediate(CToS_Request_SelectCardsCustomIntermediate value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("select_cards_custom_intermediate")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static select_cards_custom_intermediate DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/select_cards_custom_intermediate, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_SelectCardsCustomIntermediate value = CToS_Request_SelectCardsCustomIntermediate.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record select_cards_custom(CToS_Response_SelectCardsCustom value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("select_cards_custom")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static select_cards_custom DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/select_cards_custom, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Response_SelectCardsCustom value = CToS_Response_SelectCardsCustom.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record select_zone(CToS_Response_SelectZone value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("select_zone")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static select_zone DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/select_zone, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Response_SelectZone value = CToS_Response_SelectZone.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	internal record pass() : CToS_Content
	{
		public static pass DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for CToS_Content/pass, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("pass"), (byte)Common.TypeBytes.Void];
		}
	}
	internal record view_grave(CToS_Request_ViewGrave value) : CToS_Content
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("view_grave")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static view_grave DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Content/view_grave, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CToS_Request_ViewGrave value = CToS_Request_ViewGrave.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
}
internal record CToS_Request_GetActions(CardGameUtils.GameEnumsAndStructs.Location location, uint uid) : Common.PacketTable
{
	public static CToS_Request_GetActions DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header location */
		{
			if(!Common.Common.DeserializeName(ref bytes, "location")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_GetActions.location hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for CToS_Request_GetActions.location, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header uid */
		{
			if(!Common.Common.DeserializeName(ref bytes, "uid")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_GetActions.uid hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for CToS_Request_GetActions.uid, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		CardGameUtils.GameEnumsAndStructs.Location location = (CardGameUtils.GameEnumsAndStructs.Location)Common.Common.DeserializeN8(ref bytes);
		if(!Common.Common.DeserializeName(ref bytes, Enum.GetName(location)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.SerializeName(Enum.GetName(location)!))}]");
		}
		uint uid = Common.Common.DeserializeN32(ref bytes);
		return new(location, uid);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header location */
		bytes.AddRange(Common.Common.SerializeName("location")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header uid */
		bytes.AddRange(Common.Common.SerializeName("uid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Data location */
		bytes.AddRange(Common.Common.SerializeN8((byte)location));
		bytes.AddRange(Common.Common.SerializeName(Enum.GetName(location)!, len: 3));
		/* Data uid */
		bytes.AddRange(Common.Common.SerializeN32(uid));
		return bytes;
	}
}
internal record CToS_Request_SelectOption(CardGameUtils.GameEnumsAndStructs.Location location, uint uid, CardGameUtils.Base.CardAction action) : Common.PacketTable
{
	public static CToS_Request_SelectOption DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header location */
		{
			if(!Common.Common.DeserializeName(ref bytes, "location")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_SelectOption.location hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for CToS_Request_SelectOption.location, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header uid */
		{
			if(!Common.Common.DeserializeName(ref bytes, "uid")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_SelectOption.uid hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for CToS_Request_SelectOption.uid, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header action */
		{
			if(!Common.Common.DeserializeName(ref bytes, "action")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_SelectOption.action hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for CToS_Request_SelectOption.action, expected {(byte)(Common.TypeBytes.Table)}, got {type}");
			}
		}
		CardGameUtils.GameEnumsAndStructs.Location location = (CardGameUtils.GameEnumsAndStructs.Location)Common.Common.DeserializeN8(ref bytes);
		if(!Common.Common.DeserializeName(ref bytes, Enum.GetName(location)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.SerializeName(Enum.GetName(location)!))}]");
		}
		uint uid = Common.Common.DeserializeN32(ref bytes);
		CardGameUtils.Base.CardAction action = CardGameUtils.Base.CardAction.DeserializeInternal(ref bytes);
		return new(location, uid, action);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header location */
		bytes.AddRange(Common.Common.SerializeName("location")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header uid */
		bytes.AddRange(Common.Common.SerializeName("uid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header action */
		bytes.AddRange(Common.Common.SerializeName("action")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
		/* Data location */
		bytes.AddRange(Common.Common.SerializeN8((byte)location));
		bytes.AddRange(Common.Common.SerializeName(Enum.GetName(location)!, len: 3));
		/* Data uid */
		bytes.AddRange(Common.Common.SerializeN32(uid));
		/* Data action */
		bytes.AddRange(action.SerializeInternal());
		return bytes;
	}
}
internal record CToS_Response_YesNo(bool yes) : Common.PacketTable
{
	public static CToS_Response_YesNo DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header yes */
		{
			if(!Common.Common.DeserializeName(ref bytes, "yes")) /* Name */
			{
				throw new Exception("Field Header CToS_Response_YesNo.yes hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for CToS_Response_YesNo.yes, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		bool yes = Common.Common.DeserializeBool(ref bytes);
		return new(yes);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header yes */
		bytes.AddRange(Common.Common.SerializeName("yes")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data yes */
		bytes.AddRange(Common.Common.SerializeBool(yes));
		return bytes;
	}
}
internal record CToS_Response_SelectCards(List<uint> uids) : Common.PacketTable
{
	public static CToS_Response_SelectCards DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header uids */
		{
			if(!Common.Common.DeserializeName(ref bytes, "uids")) /* Name */
			{
				throw new Exception("Field Header CToS_Response_SelectCards.uids hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for CToS_Response_SelectCards.uids, expected {(byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte uidsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(uidsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for uids, expected 0, got {uidsNestingLevel}");
		}
		uint uidsCount = Common.Common.DeserializeN32(ref bytes);
		List<uint> uids = new((int)uidsCount);
		for(int uids_ = 0; uids_ < uids.Capacity; uids_++)
		{
			uids.Add(Common.Common.DeserializeN32(ref bytes));
		}
		return new(uids);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header uids */
		bytes.AddRange(Common.Common.SerializeName("uids")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag)); /* Type */
		/* Data uids */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)uids.Count)); /* Count */
		/* Nesting Counts */
		foreach(var uids_ in uids)
		{
			bytes.AddRange(Common.Common.SerializeN32(uids_));
		}
		return bytes;
	}
}
internal record CToS_Request_SelectCardsCustomIntermediate(List<uint> uids) : Common.PacketTable
{
	public static CToS_Request_SelectCardsCustomIntermediate DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header uids */
		{
			if(!Common.Common.DeserializeName(ref bytes, "uids")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_SelectCardsCustomIntermediate.uids hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for CToS_Request_SelectCardsCustomIntermediate.uids, expected {(byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte uidsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(uidsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for uids, expected 0, got {uidsNestingLevel}");
		}
		uint uidsCount = Common.Common.DeserializeN32(ref bytes);
		List<uint> uids = new((int)uidsCount);
		for(int uids_ = 0; uids_ < uids.Capacity; uids_++)
		{
			uids.Add(Common.Common.DeserializeN32(ref bytes));
		}
		return new(uids);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header uids */
		bytes.AddRange(Common.Common.SerializeName("uids")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag)); /* Type */
		/* Data uids */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)uids.Count)); /* Count */
		/* Nesting Counts */
		foreach(var uids_ in uids)
		{
			bytes.AddRange(Common.Common.SerializeN32(uids_));
		}
		return bytes;
	}
}
internal record CToS_Response_SelectCardsCustom(List<uint> uids) : Common.PacketTable
{
	public static CToS_Response_SelectCardsCustom DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header uids */
		{
			if(!Common.Common.DeserializeName(ref bytes, "uids")) /* Name */
			{
				throw new Exception("Field Header CToS_Response_SelectCardsCustom.uids hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for CToS_Response_SelectCardsCustom.uids, expected {(byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		byte uidsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(uidsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for uids, expected 0, got {uidsNestingLevel}");
		}
		uint uidsCount = Common.Common.DeserializeN32(ref bytes);
		List<uint> uids = new((int)uidsCount);
		for(int uids_ = 0; uids_ < uids.Capacity; uids_++)
		{
			uids.Add(Common.Common.DeserializeN32(ref bytes));
		}
		return new(uids);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header uids */
		bytes.AddRange(Common.Common.SerializeName("uids")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32 | Common.TypeBytes.ListFlag)); /* Type */
		/* Data uids */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)uids.Count)); /* Count */
		/* Nesting Counts */
		foreach(var uids_ in uids)
		{
			bytes.AddRange(Common.Common.SerializeN32(uids_));
		}
		return bytes;
	}
}
internal record CToS_Response_SelectZone(int zone) : Common.PacketTable
{
	public static CToS_Response_SelectZone DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header zone */
		{
			if(!Common.Common.DeserializeName(ref bytes, "zone")) /* Name */
			{
				throw new Exception("Field Header CToS_Response_SelectZone.zone hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CToS_Response_SelectZone.zone, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		int zone = Common.Common.DeserializeI32(ref bytes);
		return new(zone);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header zone */
		bytes.AddRange(Common.Common.SerializeName("zone")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Data zone */
		bytes.AddRange(Common.Common.SerializeI32(zone));
		return bytes;
	}
}
internal record CToS_Request_ViewGrave(bool for_opponent) : Common.PacketTable
{
	public static CToS_Request_ViewGrave DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header for_opponent */
		{
			if(!Common.Common.DeserializeName(ref bytes, "for_opponent")) /* Name */
			{
				throw new Exception("Field Header CToS_Request_ViewGrave.for_opponent hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for CToS_Request_ViewGrave.for_opponent, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		bool for_opponent = Common.Common.DeserializeBool(ref bytes);
		return new(for_opponent);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header for_opponent */
		bytes.AddRange(Common.Common.SerializeName("for_opponent")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data for_opponent */
		bytes.AddRange(Common.Common.SerializeBool(for_opponent));
		return bytes;
	}
}
