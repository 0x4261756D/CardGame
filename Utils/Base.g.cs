using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Base;

#nullable enable

public record CardStruct(string name, string text, CardGameUtils.GameConstants.PlayerClass card_class, CardGameUtils.GameConstants.Location location, uint uid, byte controller, byte base_controller, TypeSpecifics type_specifics) : Common.PacketTable
{
	public byte[] Deserialize()
	{
		List<byte> dataBytes = DeserializeInternal();
		return [.. Common.Common.DeserializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.DeserializeN16(2) /* ProtoVersion */,
			.. Common.Common.DeserializeN16(1) /* SchemaVersion */,
			.. Common.Common.DeserializeName("CardStruct") /* Name */,
			.. dataBytes /* Root */];
	}

	public static CardStruct Serialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.SerializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return SerializeImpl(ref bytes);
	}
	public static CardStruct Serialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.SerializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return SerializeImpl(ref bytes);
	}
	private static CardStruct SerializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.SerializeName(ref bytes, "CardStruct"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		CardStruct ret = SerializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static CardStruct SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.SerializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header CardStruct.name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CardStruct.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header text */
		{
			if(!Common.Common.SerializeName(ref bytes, "text")) /* Name */
			{
				throw new Exception("Field Header CardStruct.text hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CardStruct.text, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header card_class */
		{
			if(!Common.Common.SerializeName(ref bytes, "card_class")) /* Name */
			{
				throw new Exception("Field Header CardStruct.card_class hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for CardStruct.card_class, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header location */
		{
			if(!Common.Common.SerializeName(ref bytes, "location")) /* Name */
			{
				throw new Exception("Field Header CardStruct.location hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for CardStruct.location, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header uid */
		{
			if(!Common.Common.SerializeName(ref bytes, "uid")) /* Name */
			{
				throw new Exception("Field Header CardStruct.uid hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for CardStruct.uid, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header controller */
		{
			if(!Common.Common.SerializeName(ref bytes, "controller")) /* Name */
			{
				throw new Exception("Field Header CardStruct.controller hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N8))
			{
				throw new Exception($"Wrong field type for CardStruct.controller, expected {(byte)(Common.TypeBytes.N8)}, got {type}");
			}
		}
		/* Field Header base_controller */
		{
			if(!Common.Common.SerializeName(ref bytes, "base_controller")) /* Name */
			{
				throw new Exception("Field Header CardStruct.base_controller hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N8))
			{
				throw new Exception($"Wrong field type for CardStruct.base_controller, expected {(byte)(Common.TypeBytes.N8)}, got {type}");
			}
		}
		/* Field Header type_specifics */
		{
			if(!Common.Common.SerializeName(ref bytes, "type_specifics")) /* Name */
			{
				throw new Exception("Field Header CardStruct.type_specifics hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for CardStruct.type_specifics, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		string name = Common.Common.SerializeStr(ref bytes);
		string text = Common.Common.SerializeStr(ref bytes);
		CardGameUtils.GameConstants.PlayerClass card_class = (CardGameUtils.GameConstants.PlayerClass)Common.Common.SerializeN8(ref bytes);
		if(!Common.Common.SerializeName(ref bytes, Enum.GetName(card_class)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.DeserializeName(Enum.GetName(card_class)!))}]");
		}
		CardGameUtils.GameConstants.Location location = (CardGameUtils.GameConstants.Location)Common.Common.SerializeN8(ref bytes);
		if(!Common.Common.SerializeName(ref bytes, Enum.GetName(location)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.DeserializeName(Enum.GetName(location)!))}]");
		}
		uint uid = Common.Common.SerializeN32(ref bytes);
		byte controller = Common.Common.SerializeN8(ref bytes);
		byte base_controller = Common.Common.SerializeN8(ref bytes);
		TypeSpecifics type_specifics = TypeSpecifics.SerializeInternal(ref bytes);
		return new(name, text, card_class, location, uid, controller, base_controller, type_specifics);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.DeserializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header text */
		bytes.AddRange(Common.Common.DeserializeName("text")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header card_class */
		bytes.AddRange(Common.Common.DeserializeName("card_class")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header location */
		bytes.AddRange(Common.Common.DeserializeName("location")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header uid */
		bytes.AddRange(Common.Common.DeserializeName("uid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header controller */
		bytes.AddRange(Common.Common.DeserializeName("controller")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N8)); /* Type */
		/* Field Header base_controller */
		bytes.AddRange(Common.Common.DeserializeName("base_controller")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N8)); /* Type */
		/* Field Header type_specifics */
		bytes.AddRange(Common.Common.DeserializeName("type_specifics")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.DeserializeStr(name));
		/* Data text */
		bytes.AddRange(Common.Common.DeserializeStr(text));
		/* Data card_class */
		bytes.AddRange(Common.Common.DeserializeN8((byte)card_class));
		bytes.AddRange(Common.Common.DeserializeName(Enum.GetName(card_class)!, len: 3));
		/* Data location */
		bytes.AddRange(Common.Common.DeserializeN8((byte)location));
		bytes.AddRange(Common.Common.DeserializeName(Enum.GetName(location)!, len: 3));
		/* Data uid */
		bytes.AddRange(Common.Common.DeserializeN32(uid));
		/* Data controller */
		bytes.AddRange(Common.Common.DeserializeN8(controller));
		/* Data base_controller */
		bytes.AddRange(Common.Common.DeserializeN8(base_controller));
		/* Data type_specifics */
		bytes.AddRange(type_specifics.DeserializeInternal());
		return bytes;
	}
}
public interface TypeSpecifics : Common.PacketUnion
{
	public static TypeSpecifics SerializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.DeserializeName("creature")))
		{
			return creature.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("spell")))
		{
			return spell.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("quest")))
		{
			return quest.SerializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("unknown")))
		{
			return new unknown();
		}
		else 
		{
			throw new Exception("Unknown union variant in TypeSpecifics");
		}
	}

	public record creature(CreatureSpecifics value) : TypeSpecifics
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("creature")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static creature SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for TypeSpecifics/creature, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CreatureSpecifics value = CreatureSpecifics.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record spell(SpellSpecifics value) : TypeSpecifics
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("spell")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static spell SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for TypeSpecifics/spell, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SpellSpecifics value = SpellSpecifics.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record quest(QuestSpecifics value) : TypeSpecifics
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("quest")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.DeserializeInternal());
			return bytes;
		}

		public static quest SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for TypeSpecifics/quest, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			QuestSpecifics value = QuestSpecifics.SerializeInternal(ref bytes);
			return new(value);
		}
	}
	public record unknown() : TypeSpecifics
	{
		public static unknown SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for TypeSpecifics/unknown, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> DeserializeInternal()
		{
			return [];
		}
	}
}
public record CreatureSpecifics(int base_cost, int cost, int base_life, int life, int base_power, int power, int position, int damage_cap) : Common.PacketTable
{
	public static CreatureSpecifics SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header base_cost */
		{
			if(!Common.Common.SerializeName(ref bytes, "base_cost")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.base_cost hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.base_cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header cost */
		{
			if(!Common.Common.SerializeName(ref bytes, "cost")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.cost hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header base_life */
		{
			if(!Common.Common.SerializeName(ref bytes, "base_life")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.base_life hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.base_life, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header life */
		{
			if(!Common.Common.SerializeName(ref bytes, "life")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.life hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.life, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header base_power */
		{
			if(!Common.Common.SerializeName(ref bytes, "base_power")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.base_power hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.base_power, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header power */
		{
			if(!Common.Common.SerializeName(ref bytes, "power")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.power hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.power, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header position */
		{
			if(!Common.Common.SerializeName(ref bytes, "position")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.position hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.position, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header damage_cap */
		{
			if(!Common.Common.SerializeName(ref bytes, "damage_cap")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.damage_cap hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.damage_cap, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		int base_cost = Common.Common.SerializeI32(ref bytes);
		int cost = Common.Common.SerializeI32(ref bytes);
		int base_life = Common.Common.SerializeI32(ref bytes);
		int life = Common.Common.SerializeI32(ref bytes);
		int base_power = Common.Common.SerializeI32(ref bytes);
		int power = Common.Common.SerializeI32(ref bytes);
		int position = Common.Common.SerializeI32(ref bytes);
		int damage_cap = Common.Common.SerializeI32(ref bytes);
		return new(base_cost, cost, base_life, life, base_power, power, position, damage_cap);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header base_cost */
		bytes.AddRange(Common.Common.DeserializeName("base_cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header cost */
		bytes.AddRange(Common.Common.DeserializeName("cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header base_life */
		bytes.AddRange(Common.Common.DeserializeName("base_life")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header life */
		bytes.AddRange(Common.Common.DeserializeName("life")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header base_power */
		bytes.AddRange(Common.Common.DeserializeName("base_power")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header power */
		bytes.AddRange(Common.Common.DeserializeName("power")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header position */
		bytes.AddRange(Common.Common.DeserializeName("position")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header damage_cap */
		bytes.AddRange(Common.Common.DeserializeName("damage_cap")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Data base_cost */
		bytes.AddRange(Common.Common.DeserializeI32(base_cost));
		/* Data cost */
		bytes.AddRange(Common.Common.DeserializeI32(cost));
		/* Data base_life */
		bytes.AddRange(Common.Common.DeserializeI32(base_life));
		/* Data life */
		bytes.AddRange(Common.Common.DeserializeI32(life));
		/* Data base_power */
		bytes.AddRange(Common.Common.DeserializeI32(base_power));
		/* Data power */
		bytes.AddRange(Common.Common.DeserializeI32(power));
		/* Data position */
		bytes.AddRange(Common.Common.DeserializeI32(position));
		/* Data damage_cap */
		bytes.AddRange(Common.Common.DeserializeI32(damage_cap));
		return bytes;
	}
}
public record SpellSpecifics(int base_cost, int cost, bool is_class_ability, bool can_be_class_ability) : Common.PacketTable
{
	public static SpellSpecifics SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header base_cost */
		{
			if(!Common.Common.SerializeName(ref bytes, "base_cost")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.base_cost hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.base_cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header cost */
		{
			if(!Common.Common.SerializeName(ref bytes, "cost")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.cost hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header is_class_ability */
		{
			if(!Common.Common.SerializeName(ref bytes, "is_class_ability")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.is_class_ability hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.is_class_ability, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		/* Field Header can_be_class_ability */
		{
			if(!Common.Common.SerializeName(ref bytes, "can_be_class_ability")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.can_be_class_ability hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.can_be_class_ability, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		int base_cost = Common.Common.SerializeI32(ref bytes);
		int cost = Common.Common.SerializeI32(ref bytes);
		bool is_class_ability = Common.Common.SerializeBool(ref bytes);
		bool can_be_class_ability = Common.Common.SerializeBool(ref bytes);
		return new(base_cost, cost, is_class_ability, can_be_class_ability);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header base_cost */
		bytes.AddRange(Common.Common.DeserializeName("base_cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header cost */
		bytes.AddRange(Common.Common.DeserializeName("cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header is_class_ability */
		bytes.AddRange(Common.Common.DeserializeName("is_class_ability")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Field Header can_be_class_ability */
		bytes.AddRange(Common.Common.DeserializeName("can_be_class_ability")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data base_cost */
		bytes.AddRange(Common.Common.DeserializeI32(base_cost));
		/* Data cost */
		bytes.AddRange(Common.Common.DeserializeI32(cost));
		/* Data is_class_ability */
		bytes.AddRange(Common.Common.DeserializeBool(is_class_ability));
		/* Data can_be_class_ability */
		bytes.AddRange(Common.Common.DeserializeBool(can_be_class_ability));
		return bytes;
	}
}
public record QuestSpecifics(int progress, int goal) : Common.PacketTable
{
	public static QuestSpecifics SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header progress */
		{
			if(!Common.Common.SerializeName(ref bytes, "progress")) /* Name */
			{
				throw new Exception("Field Header QuestSpecifics.progress hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for QuestSpecifics.progress, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header goal */
		{
			if(!Common.Common.SerializeName(ref bytes, "goal")) /* Name */
			{
				throw new Exception("Field Header QuestSpecifics.goal hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for QuestSpecifics.goal, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		int progress = Common.Common.SerializeI32(ref bytes);
		int goal = Common.Common.SerializeI32(ref bytes);
		return new(progress, goal);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header progress */
		bytes.AddRange(Common.Common.DeserializeName("progress")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header goal */
		bytes.AddRange(Common.Common.DeserializeName("goal")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Data progress */
		bytes.AddRange(Common.Common.DeserializeI32(progress));
		/* Data goal */
		bytes.AddRange(Common.Common.DeserializeI32(goal));
		return bytes;
	}
}
public record CardAction(uint uid, string description) : Common.PacketTable
{
	public static CardAction SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header uid */
		{
			if(!Common.Common.SerializeName(ref bytes, "uid")) /* Name */
			{
				throw new Exception("Field Header CardAction.uid hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for CardAction.uid, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header description */
		{
			if(!Common.Common.SerializeName(ref bytes, "description")) /* Name */
			{
				throw new Exception("Field Header CardAction.description hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CardAction.description, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		uint uid = Common.Common.SerializeN32(ref bytes);
		string description = Common.Common.SerializeStr(ref bytes);
		return new(uid, description);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header uid */
		bytes.AddRange(Common.Common.DeserializeName("uid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header description */
		bytes.AddRange(Common.Common.DeserializeName("description")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data uid */
		bytes.AddRange(Common.Common.DeserializeN32(uid));
		/* Data description */
		bytes.AddRange(Common.Common.DeserializeStr(description));
		return bytes;
	}
}
public record Deck(string name, List<CardStruct> cards, CardGameUtils.GameConstants.PlayerClass player_class, CardStruct? ability, CardStruct? quest) : Common.PacketTable
{
	public static Deck SerializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.SerializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header Deck.name hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for Deck.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header cards */
		{
			if(!Common.Common.SerializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header Deck.cards hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Deck.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header player_class */
		{
			if(!Common.Common.SerializeName(ref bytes, "player_class")) /* Name */
			{
				throw new Exception("Field Header Deck.player_class hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for Deck.player_class, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header ability */
		{
			if(!Common.Common.SerializeName(ref bytes, "ability")) /* Name */
			{
				throw new Exception("Field Header Deck.ability hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for Deck.ability, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		/* Field Header quest */
		{
			if(!Common.Common.SerializeName(ref bytes, "quest")) /* Name */
			{
				throw new Exception("Field Header Deck.quest hash mismatch");
			}
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for Deck.quest, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		string name = Common.Common.SerializeStr(ref bytes);
		byte cardsNestingLevel = Common.Common.SerializeN8(ref bytes);
		if(cardsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cards, expected 0, got {cardsNestingLevel}");
		}
		uint cardsCount = Common.Common.SerializeN32(ref bytes);
		List<CardStruct> cards = new((int)cardsCount);
		for(int cards_ = 0; cards_ < cards.Capacity; cards_++)
		{
			cards.Add(CardStruct.SerializeInternal(ref bytes));
		}
		CardGameUtils.GameConstants.PlayerClass player_class = (CardGameUtils.GameConstants.PlayerClass)Common.Common.SerializeN8(ref bytes);
		if(!Common.Common.SerializeName(ref bytes, Enum.GetName(player_class)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.DeserializeName(Enum.GetName(player_class)!))}]");
		}
		CardStruct? ability = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			ability = CardStruct.SerializeInternal(ref bytes);
		}
		CardStruct? quest = null;
		if(Common.Common.SerializeBool(ref bytes))
		{
			quest = CardStruct.SerializeInternal(ref bytes);
		}
		return new(name, cards, player_class, ability, quest);
	}

	public List<byte> DeserializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.DeserializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header cards */
		bytes.AddRange(Common.Common.DeserializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header player_class */
		bytes.AddRange(Common.Common.DeserializeName("player_class")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header ability */
		bytes.AddRange(Common.Common.DeserializeName("ability")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Field Header quest */
		bytes.AddRange(Common.Common.DeserializeName("quest")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.DeserializeStr(name));
		/* Data cards */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.DeserializeN32((uint)cards.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cards_ in cards)
		{
			bytes.AddRange(cards_.DeserializeInternal());
		}
		/* Data player_class */
		bytes.AddRange(Common.Common.DeserializeN8((byte)player_class));
		bytes.AddRange(Common.Common.DeserializeName(Enum.GetName(player_class)!, len: 3));
		/* Data ability */
		if(ability is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(ability.DeserializeInternal());
		}
		/* Data quest */
		if(quest is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(quest.DeserializeInternal());
		}
		return bytes;
	}
}
public interface ErrorOr : Common.PacketUnion
{
	public static ErrorOr SerializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.DeserializeName("success")))
		{
			return new success();
		}
		else if(nameSpan.SequenceEqual(Common.Common.DeserializeName("error")))
		{
			return error.SerializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in ErrorOr");
		}
	}

	public record success() : ErrorOr
	{
		public static success SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for ErrorOr/success, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> DeserializeInternal()
		{
			return [];
		}
	}
	public record error(string value) : ErrorOr
	{
		public List<byte> DeserializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.DeserializeName("error")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
			bytes.AddRange(Common.Common.DeserializeStr(value));
			return bytes;
		}

		public static error SerializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.SerializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for ErrorOr/error, expected `{(byte)(Common.TypeBytes.Str)}`, got `{type}`");
			}
			string value = Common.Common.SerializeStr(ref bytes);
			return new(value);
		}
	}
}
