using System;
using System.IO;
using System.Collections.Generic;

namespace CardGameUtils.Base;

#nullable enable
#pragma warning disable CS8981
public record CardStruct(string name, string text, CardGameUtils.GameConstants.PlayerClass card_class, CardGameUtils.GameConstants.Location location, uint uid, int controller, int base_controller, TypeSpecifics type_specifics) : Common.PacketTable
{
	public byte[] Serialize()
	{
		List<byte> dataBytes = SerializeInternal();
		return [.. Common.Common.SerializeN32((uint)dataBytes.Count + 8) /* Size */,
			.. Common.Common.SerializeN16(2) /* ProtoVersion */,
			.. Common.Common.SerializeN16(1) /* SchemaVersion */,
			.. Common.Common.SerializeName("CardStruct") /* Name */,
			.. dataBytes /* Root */];
	}

	public static CardStruct Deserialize(byte[] packet)
	{
		Span<byte> bytes = packet;
		uint size = Common.Common.DeserializeN32(ref bytes);
		if(size != bytes.Length)
		{
			throw new Exception($"Incorrect size, expected {size}, got {bytes.Length}");
		}
		return DeserializeImpl(ref bytes);
	}
	public static CardStruct Deserialize(Stream stream)
	{
		Span<byte> sizeSpan = new byte[4];
		stream.ReadExactly(sizeSpan);
		uint size = Common.Common.DeserializeN32(ref sizeSpan);
		Span<byte> bytes = new byte[size];
		stream.ReadExactly(bytes);
		return DeserializeImpl(ref bytes);
	}
	private static CardStruct DeserializeImpl(ref Span<byte> bytes)
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
		if(!Common.Common.DeserializeName(ref bytes, "CardStruct"))
		{
			throw new Exception($"Packet name hash mismatch");
		}
		CardStruct ret = DeserializeInternal(ref bytes);
		if(bytes.Length != 0)
		{
			throw new Exception($"Internal error, after successfully serializing the packet there are still {bytes.Length} bytes left: [{string.Join(',', bytes.ToArray())}]");
		}
		return ret;
	}

	public static CardStruct DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header CardStruct.name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CardStruct.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header text */
		{
			if(!Common.Common.DeserializeName(ref bytes, "text")) /* Name */
			{
				throw new Exception("Field Header CardStruct.text hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CardStruct.text, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header card_class */
		{
			if(!Common.Common.DeserializeName(ref bytes, "card_class")) /* Name */
			{
				throw new Exception("Field Header CardStruct.card_class hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for CardStruct.card_class, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header location */
		{
			if(!Common.Common.DeserializeName(ref bytes, "location")) /* Name */
			{
				throw new Exception("Field Header CardStruct.location hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for CardStruct.location, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header uid */
		{
			if(!Common.Common.DeserializeName(ref bytes, "uid")) /* Name */
			{
				throw new Exception("Field Header CardStruct.uid hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for CardStruct.uid, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header controller */
		{
			if(!Common.Common.DeserializeName(ref bytes, "controller")) /* Name */
			{
				throw new Exception("Field Header CardStruct.controller hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CardStruct.controller, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header base_controller */
		{
			if(!Common.Common.DeserializeName(ref bytes, "base_controller")) /* Name */
			{
				throw new Exception("Field Header CardStruct.base_controller hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CardStruct.base_controller, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header type_specifics */
		{
			if(!Common.Common.DeserializeName(ref bytes, "type_specifics")) /* Name */
			{
				throw new Exception("Field Header CardStruct.type_specifics hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Union))
			{
				throw new Exception($"Wrong field type for CardStruct.type_specifics, expected {(byte)(Common.TypeBytes.Union)}, got {type}");
			}
		}
		string name = Common.Common.DeserializeStr(ref bytes);
		string text = Common.Common.DeserializeStr(ref bytes);
		CardGameUtils.GameConstants.PlayerClass card_class = (CardGameUtils.GameConstants.PlayerClass)Common.Common.DeserializeN8(ref bytes);
		if(!Common.Common.DeserializeName(ref bytes, Enum.GetName(card_class)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.SerializeName(Enum.GetName(card_class)!))}]");
		}
		CardGameUtils.GameConstants.Location location = (CardGameUtils.GameConstants.Location)Common.Common.DeserializeN8(ref bytes);
		if(!Common.Common.DeserializeName(ref bytes, Enum.GetName(location)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.SerializeName(Enum.GetName(location)!))}]");
		}
		uint uid = Common.Common.DeserializeN32(ref bytes);
		int controller = Common.Common.DeserializeI32(ref bytes);
		int base_controller = Common.Common.DeserializeI32(ref bytes);
		TypeSpecifics type_specifics = TypeSpecifics.DeserializeInternal(ref bytes);
		return new(name, text, card_class, location, uid, controller, base_controller, type_specifics);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.SerializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header text */
		bytes.AddRange(Common.Common.SerializeName("text")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header card_class */
		bytes.AddRange(Common.Common.SerializeName("card_class")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header location */
		bytes.AddRange(Common.Common.SerializeName("location")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header uid */
		bytes.AddRange(Common.Common.SerializeName("uid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header controller */
		bytes.AddRange(Common.Common.SerializeName("controller")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header base_controller */
		bytes.AddRange(Common.Common.SerializeName("base_controller")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header type_specifics */
		bytes.AddRange(Common.Common.SerializeName("type_specifics")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Union)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.SerializeStr(name));
		/* Data text */
		bytes.AddRange(Common.Common.SerializeStr(text));
		/* Data card_class */
		bytes.AddRange(Common.Common.SerializeN8((byte)card_class));
		bytes.AddRange(Common.Common.SerializeName(Enum.GetName(card_class)!, len: 3));
		/* Data location */
		bytes.AddRange(Common.Common.SerializeN8((byte)location));
		bytes.AddRange(Common.Common.SerializeName(Enum.GetName(location)!, len: 3));
		/* Data uid */
		bytes.AddRange(Common.Common.SerializeN32(uid));
		/* Data controller */
		bytes.AddRange(Common.Common.SerializeI32(controller));
		/* Data base_controller */
		bytes.AddRange(Common.Common.SerializeI32(base_controller));
		/* Data type_specifics */
		bytes.AddRange(type_specifics.SerializeInternal());
		return bytes;
	}
}
public interface TypeSpecifics : Common.PacketUnion
{
	public static TypeSpecifics DeserializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.SerializeName("creature")))
		{
			return creature.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("spell")))
		{
			return spell.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("quest")))
		{
			return quest.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("unknown")))
		{
			return unknown.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in TypeSpecifics");
		}
	}

	public record creature(CreatureSpecifics value) : TypeSpecifics
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("creature")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static creature DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for TypeSpecifics/creature, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			CreatureSpecifics value = CreatureSpecifics.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record spell(SpellSpecifics value) : TypeSpecifics
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("spell")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static spell DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for TypeSpecifics/spell, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			SpellSpecifics value = SpellSpecifics.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record quest(QuestSpecifics value) : TypeSpecifics
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("quest")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Table)); /* Type */
			bytes.AddRange(value.SerializeInternal());
			return bytes;
		}

		public static quest DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table))
			{
				throw new Exception($"Wrong field type for TypeSpecifics/quest, expected `{(byte)(Common.TypeBytes.Table)}`, got `{type}`");
			}
			QuestSpecifics value = QuestSpecifics.DeserializeInternal(ref bytes);
			return new(value);
		}
	}
	public record unknown() : TypeSpecifics
	{
		public static unknown DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for TypeSpecifics/unknown, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("unknown"), (byte)Common.TypeBytes.Void];
		}
	}
}
public record CreatureSpecifics(int base_cost, int cost, int base_life, int life, int base_power, int power, int position, int damage_cap) : Common.PacketTable
{
	public static CreatureSpecifics DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header base_cost */
		{
			if(!Common.Common.DeserializeName(ref bytes, "base_cost")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.base_cost hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.base_cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header cost */
		{
			if(!Common.Common.DeserializeName(ref bytes, "cost")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.cost hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header base_life */
		{
			if(!Common.Common.DeserializeName(ref bytes, "base_life")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.base_life hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.base_life, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header life */
		{
			if(!Common.Common.DeserializeName(ref bytes, "life")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.life hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.life, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header base_power */
		{
			if(!Common.Common.DeserializeName(ref bytes, "base_power")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.base_power hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.base_power, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header power */
		{
			if(!Common.Common.DeserializeName(ref bytes, "power")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.power hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.power, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header position */
		{
			if(!Common.Common.DeserializeName(ref bytes, "position")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.position hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.position, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header damage_cap */
		{
			if(!Common.Common.DeserializeName(ref bytes, "damage_cap")) /* Name */
			{
				throw new Exception("Field Header CreatureSpecifics.damage_cap hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for CreatureSpecifics.damage_cap, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		int base_cost = Common.Common.DeserializeI32(ref bytes);
		int cost = Common.Common.DeserializeI32(ref bytes);
		int base_life = Common.Common.DeserializeI32(ref bytes);
		int life = Common.Common.DeserializeI32(ref bytes);
		int base_power = Common.Common.DeserializeI32(ref bytes);
		int power = Common.Common.DeserializeI32(ref bytes);
		int position = Common.Common.DeserializeI32(ref bytes);
		int damage_cap = Common.Common.DeserializeI32(ref bytes);
		return new(base_cost, cost, base_life, life, base_power, power, position, damage_cap);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header base_cost */
		bytes.AddRange(Common.Common.SerializeName("base_cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header cost */
		bytes.AddRange(Common.Common.SerializeName("cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header base_life */
		bytes.AddRange(Common.Common.SerializeName("base_life")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header life */
		bytes.AddRange(Common.Common.SerializeName("life")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header base_power */
		bytes.AddRange(Common.Common.SerializeName("base_power")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header power */
		bytes.AddRange(Common.Common.SerializeName("power")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header position */
		bytes.AddRange(Common.Common.SerializeName("position")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header damage_cap */
		bytes.AddRange(Common.Common.SerializeName("damage_cap")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Data base_cost */
		bytes.AddRange(Common.Common.SerializeI32(base_cost));
		/* Data cost */
		bytes.AddRange(Common.Common.SerializeI32(cost));
		/* Data base_life */
		bytes.AddRange(Common.Common.SerializeI32(base_life));
		/* Data life */
		bytes.AddRange(Common.Common.SerializeI32(life));
		/* Data base_power */
		bytes.AddRange(Common.Common.SerializeI32(base_power));
		/* Data power */
		bytes.AddRange(Common.Common.SerializeI32(power));
		/* Data position */
		bytes.AddRange(Common.Common.SerializeI32(position));
		/* Data damage_cap */
		bytes.AddRange(Common.Common.SerializeI32(damage_cap));
		return bytes;
	}
}
public record SpellSpecifics(int base_cost, int cost, bool is_class_ability, bool can_be_class_ability) : Common.PacketTable
{
	public static SpellSpecifics DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header base_cost */
		{
			if(!Common.Common.DeserializeName(ref bytes, "base_cost")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.base_cost hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.base_cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header cost */
		{
			if(!Common.Common.DeserializeName(ref bytes, "cost")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.cost hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.cost, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header is_class_ability */
		{
			if(!Common.Common.DeserializeName(ref bytes, "is_class_ability")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.is_class_ability hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.is_class_ability, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		/* Field Header can_be_class_ability */
		{
			if(!Common.Common.DeserializeName(ref bytes, "can_be_class_ability")) /* Name */
			{
				throw new Exception("Field Header SpellSpecifics.can_be_class_ability hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Bool))
			{
				throw new Exception($"Wrong field type for SpellSpecifics.can_be_class_ability, expected {(byte)(Common.TypeBytes.Bool)}, got {type}");
			}
		}
		int base_cost = Common.Common.DeserializeI32(ref bytes);
		int cost = Common.Common.DeserializeI32(ref bytes);
		bool is_class_ability = Common.Common.DeserializeBool(ref bytes);
		bool can_be_class_ability = Common.Common.DeserializeBool(ref bytes);
		return new(base_cost, cost, is_class_ability, can_be_class_ability);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header base_cost */
		bytes.AddRange(Common.Common.SerializeName("base_cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header cost */
		bytes.AddRange(Common.Common.SerializeName("cost")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header is_class_ability */
		bytes.AddRange(Common.Common.SerializeName("is_class_ability")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Field Header can_be_class_ability */
		bytes.AddRange(Common.Common.SerializeName("can_be_class_ability")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Bool)); /* Type */
		/* Data base_cost */
		bytes.AddRange(Common.Common.SerializeI32(base_cost));
		/* Data cost */
		bytes.AddRange(Common.Common.SerializeI32(cost));
		/* Data is_class_ability */
		bytes.AddRange(Common.Common.SerializeBool(is_class_ability));
		/* Data can_be_class_ability */
		bytes.AddRange(Common.Common.SerializeBool(can_be_class_ability));
		return bytes;
	}
}
public record QuestSpecifics(int progress, int goal) : Common.PacketTable
{
	public static QuestSpecifics DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header progress */
		{
			if(!Common.Common.DeserializeName(ref bytes, "progress")) /* Name */
			{
				throw new Exception("Field Header QuestSpecifics.progress hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for QuestSpecifics.progress, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		/* Field Header goal */
		{
			if(!Common.Common.DeserializeName(ref bytes, "goal")) /* Name */
			{
				throw new Exception("Field Header QuestSpecifics.goal hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.I32))
			{
				throw new Exception($"Wrong field type for QuestSpecifics.goal, expected {(byte)(Common.TypeBytes.I32)}, got {type}");
			}
		}
		int progress = Common.Common.DeserializeI32(ref bytes);
		int goal = Common.Common.DeserializeI32(ref bytes);
		return new(progress, goal);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header progress */
		bytes.AddRange(Common.Common.SerializeName("progress")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Field Header goal */
		bytes.AddRange(Common.Common.SerializeName("goal")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.I32)); /* Type */
		/* Data progress */
		bytes.AddRange(Common.Common.SerializeI32(progress));
		/* Data goal */
		bytes.AddRange(Common.Common.SerializeI32(goal));
		return bytes;
	}
}
public record CardAction(uint uid, string description) : Common.PacketTable
{
	public static CardAction DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header uid */
		{
			if(!Common.Common.DeserializeName(ref bytes, "uid")) /* Name */
			{
				throw new Exception("Field Header CardAction.uid hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.N32))
			{
				throw new Exception($"Wrong field type for CardAction.uid, expected {(byte)(Common.TypeBytes.N32)}, got {type}");
			}
		}
		/* Field Header description */
		{
			if(!Common.Common.DeserializeName(ref bytes, "description")) /* Name */
			{
				throw new Exception("Field Header CardAction.description hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for CardAction.description, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		uint uid = Common.Common.DeserializeN32(ref bytes);
		string description = Common.Common.DeserializeStr(ref bytes);
		return new(uid, description);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header uid */
		bytes.AddRange(Common.Common.SerializeName("uid")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.N32)); /* Type */
		/* Field Header description */
		bytes.AddRange(Common.Common.SerializeName("description")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Data uid */
		bytes.AddRange(Common.Common.SerializeN32(uid));
		/* Data description */
		bytes.AddRange(Common.Common.SerializeStr(description));
		return bytes;
	}
}
public record Deck(string name, List<CardStruct> cards, CardGameUtils.GameConstants.PlayerClass player_class, CardStruct? ability, CardStruct? quest) : Common.PacketTable
{
	public static Deck DeserializeInternal(ref Span<byte> bytes)
	{
		/* Field Header name */
		{
			if(!Common.Common.DeserializeName(ref bytes, "name")) /* Name */
			{
				throw new Exception("Field Header Deck.name hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for Deck.name, expected {(byte)(Common.TypeBytes.Str)}, got {type}");
			}
		}
		/* Field Header cards */
		{
			if(!Common.Common.DeserializeName(ref bytes, "cards")) /* Name */
			{
				throw new Exception("Field Header Deck.cards hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag))
			{
				throw new Exception($"Wrong field type for Deck.cards, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)}, got {type}");
			}
		}
		/* Field Header player_class */
		{
			if(!Common.Common.DeserializeName(ref bytes, "player_class")) /* Name */
			{
				throw new Exception("Field Header Deck.player_class hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Enum))
			{
				throw new Exception($"Wrong field type for Deck.player_class, expected {(byte)(Common.TypeBytes.Enum)}, got {type}");
			}
		}
		/* Field Header ability */
		{
			if(!Common.Common.DeserializeName(ref bytes, "ability")) /* Name */
			{
				throw new Exception("Field Header Deck.ability hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for Deck.ability, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		/* Field Header quest */
		{
			if(!Common.Common.DeserializeName(ref bytes, "quest")) /* Name */
			{
				throw new Exception("Field Header Deck.quest hash mismatch");
			}
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag))
			{
				throw new Exception($"Wrong field type for Deck.quest, expected {(byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)}, got {type}");
			}
		}
		string name = Common.Common.DeserializeStr(ref bytes);
		byte cardsNestingLevel = Common.Common.DeserializeN8(ref bytes);
		if(cardsNestingLevel != 0)
		{
			throw new Exception("Wrong nesting level for cards, expected 0, got {cardsNestingLevel}");
		}
		uint cardsCount = Common.Common.DeserializeN32(ref bytes);
		List<CardStruct> cards = new((int)cardsCount);
		for(int cards_ = 0; cards_ < cards.Capacity; cards_++)
		{
			cards.Add(CardStruct.DeserializeInternal(ref bytes));
		}
		CardGameUtils.GameConstants.PlayerClass player_class = (CardGameUtils.GameConstants.PlayerClass)Common.Common.DeserializeN8(ref bytes);
		if(!Common.Common.DeserializeName(ref bytes, Enum.GetName(player_class)!, len: 3))
		{
			throw new Exception($"Wrong enum name hash, got [{string.Join(',', Common.Common.SerializeName(Enum.GetName(player_class)!))}]");
		}
		CardStruct? ability = null;
		if(Common.Common.DeserializeBool(ref bytes))
		{
			ability = CardStruct.DeserializeInternal(ref bytes);
		}
		CardStruct? quest = null;
		if(Common.Common.DeserializeBool(ref bytes))
		{
			quest = CardStruct.DeserializeInternal(ref bytes);
		}
		return new(name, cards, player_class, ability, quest);
	}

	public List<byte> SerializeInternal()
	{
		List<byte> bytes = [];
		/* Field Header name */
		bytes.AddRange(Common.Common.SerializeName("name")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
		/* Field Header cards */
		bytes.AddRange(Common.Common.SerializeName("cards")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.ListFlag)); /* Type */
		/* Field Header player_class */
		bytes.AddRange(Common.Common.SerializeName("player_class")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Enum)); /* Type */
		/* Field Header ability */
		bytes.AddRange(Common.Common.SerializeName("ability")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Field Header quest */
		bytes.AddRange(Common.Common.SerializeName("quest")); /* Name */
		bytes.Add((byte)(Common.TypeBytes.Table | Common.TypeBytes.OptionalFlag)); /* Type */
		/* Data name */
		bytes.AddRange(Common.Common.SerializeStr(name));
		/* Data cards */
		bytes.Add(0); /* Nesting level */
		bytes.AddRange(Common.Common.SerializeN32((uint)cards.Count)); /* Count */
		/* Nesting Counts */
		foreach(var cards_ in cards)
		{
			bytes.AddRange(cards_.SerializeInternal());
		}
		/* Data player_class */
		bytes.AddRange(Common.Common.SerializeN8((byte)player_class));
		bytes.AddRange(Common.Common.SerializeName(Enum.GetName(player_class)!, len: 3));
		/* Data ability */
		if(ability is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(ability.SerializeInternal());
		}
		/* Data quest */
		if(quest is null)
		{
			bytes.Add(0); /* IsSet */
		}
		else
		{
			bytes.Add(1); /* IsSet */
			bytes.AddRange(quest.SerializeInternal());
		}
		return bytes;
	}
}
public interface ErrorOr : Common.PacketUnion
{
	public static ErrorOr DeserializeInternal(ref Span<byte> bytes)
	{
		Span<byte> nameSpan = bytes[..4];
		bytes = bytes[4..];
		if(nameSpan.SequenceEqual(Common.Common.SerializeName("success")))
		{
			return success.DeserializeInternal(ref bytes);
		}
		else if(nameSpan.SequenceEqual(Common.Common.SerializeName("error")))
		{
			return error.DeserializeInternal(ref bytes);
		}
		else 
		{
			throw new Exception("Unknown union variant in ErrorOr");
		}
	}

	public record success() : ErrorOr
	{
		public static success DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)Common.TypeBytes.Void)
			{
				throw new Exception("Wrong field type for ErrorOr/success, expected `{(byte)Common.TypeBytes.Void}`, got `type`");
			}
			return new();
		}
		public List<byte> SerializeInternal()
		{
			return [.. Common.Common.SerializeName("success"), (byte)Common.TypeBytes.Void];
		}
	}
	public record error(string value) : ErrorOr
	{
		public List<byte> SerializeInternal()
		{
			List<byte> bytes = [];
			bytes.AddRange(Common.Common.SerializeName("error")); /* Name */
			bytes.Add((byte)(Common.TypeBytes.Str)); /* Type */
			bytes.AddRange(Common.Common.SerializeStr(value));
			return bytes;
		}

		public static error DeserializeInternal(ref Span<byte> bytes)
		{
			byte type = Common.Common.DeserializeN8(ref bytes);
			if(type != (byte)(Common.TypeBytes.Str))
			{
				throw new Exception($"Wrong field type for ErrorOr/error, expected `{(byte)(Common.TypeBytes.Str)}`, got `{type}`");
			}
			string value = Common.Common.DeserializeStr(ref bytes);
			return new(value);
		}
	}
}
