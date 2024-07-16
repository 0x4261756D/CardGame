using System.Collections.Generic;
using CardGameUtils.Shared;
using CardGameUtils.Constants;

namespace CardGameCore;

class Grave
{
	private readonly List<Card> cards = [];
	public int Size
	{
		get => cards.Count;
	}

	public Grave()
	{

	}

	internal void Add(Card card)
	{
		if(card.CardType == TypeSpecifics.creature && ((Creature)card).Keywords.ContainsKey(Keyword.Token))
		{
			return;
		}
		card.Location = Location.Grave;
		card.ResetToBaseState();
		cards.Add(card);
	}

	internal Card[] GetAll()
	{
		return [.. cards];
	}

	internal void Remove(Card card)
	{
		_ = cards.Remove(card);
	}
}
