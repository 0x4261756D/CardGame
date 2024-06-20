using System.Collections.Generic;
using CardGameUtils.CardConstants;

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

	internal CardInfo[] ToStruct()
	{
		return [.. cards.ConvertAll(card => card.ToStruct())];
	}

	internal Card[] GetAll()
	{
		return [.. cards];
	}

	internal void Remove(Card c)
	{
		// NOTE: This could be the wrong workaround, if something like a card cast from nowhere sticks in hand, look here
		if(c.Location != Location.Hand)
		{
			c.Location &= ~Location.Hand;
		}
		_ = cards.Remove(c);
	}

	internal Card[] GetDiscardable(Card? ignore)
	{
		return [.. cards.FindAll(card => card.uid != ignore?.uid && card.CanBeDiscarded())];
	}

	internal CardInfo[] ToHiddenStruct()
	{
		return [.. cards.ConvertAll(x => new CardInfo())];
	}

	internal Card GetByUID(int uid)
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
