using System.Collections.Generic;
using CardGameUtils.GameEnumsAndStructs;
using CardGameUtils.Base;

namespace CardGameCore;

class Hand
{
	private readonly List<Card> cards = [];
	public Hand()
	{
	}

	public int Count
	{
		get => cards.Count;
	}

	public void Add(Card c)
	{
		c.Location = Location.Hand;
		cards.Add(c);
	}

	internal CardStruct[] ToStruct()
	{
		return [.. cards.ConvertAll(card => card.ToStruct())];
	}

	internal Card[] GetAll()
	{
		return [.. cards];
	}

	internal void Remove(Card c)
	{
		_ = cards.Remove(c);
	}

	internal Card[] GetDiscardable(Card? ignore)
	{
		return [.. cards.FindAll(card => card.uid != ignore?.uid && card.CanBeDiscarded())];
	}

	internal CardStruct[] ToHiddenStruct()
	{
		return [.. cards.ConvertAll(x => new CardStruct(name: "UNKNOWN", text: "UNKNOWN", card_class: PlayerClass.UNKNOWN, location: Location.Hand, uid: x.uid, controller: x.Controller, base_controller: x.BaseController, type_specifics: new TypeSpecifics.unknown()))];
	}

	internal Card GetByUID(uint uid)
	{
		return cards[cards.FindIndex(x => x.uid == uid)];
	}

	internal void ClearCardModifications()
	{
		foreach(Card card in cards)
		{
			card.ResetToBaseState();
		}
	}
}
