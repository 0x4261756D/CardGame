using System;
using System.Collections.Generic;
using CardGameUtils.GameEnumsAndStructs;

namespace CardGameCore;

class Deck
{
	private readonly List<Card> cards = [];
	private readonly List<Card> revealedCards = [];
	public Deck()
	{

	}

	public int Size => cards.Count;

	internal void Add(Card c)
	{
		c.Location = Location.Deck;
		cards.Add(c);
	}

	internal Card? Pop()
	{
		if(cards.Count == 0)
		{
			return null;
		}
		Card ret = cards[0];
		cards.RemoveAt(0);
		ret.Location = Location.UNKNOWN;
		return ret;
	}

	internal Card GetAt(int position)
	{
		return cards[position];
	}

	internal void MoveToBottom(int position)
	{
		Card c = cards[position];
		cards.RemoveAt(position);
		cards.Add(c);
	}

	internal Card[] GetRange(int position, int amount)
	{
		return [.. cards[position..Math.Min(amount, cards.Count)]];
	}

	internal void Remove(Card card)
	{
		if(!cards.Remove(card) && !revealedCards.Remove(card))
		{
			throw new Exception($"Tried to remove nonexistent card {card} from the deck");
		}
	}

	internal void Shuffle()
	{
		for(int i = cards.Count - 1; i >= 0; i--)
		{
			int k = DuelCore.rnd.Next(i);
			(cards[k], cards[i]) = (cards[i], cards[k]);
		}
	}

	internal void PopRevealedAndShuffle()
	{
		cards.AddRange(revealedCards);
		revealedCards.Clear();
		Shuffle();
	}

	internal void PushToRevealed()
	{
		revealedCards.Add(cards[0]);
		cards.RemoveAt(0);
	}
}
