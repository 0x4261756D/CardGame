using System.Collections.Generic;
using CardGameUtils;
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
		if(card.CardType == GameConstants.CardType.Creature && ((Creature)card).Keywords.ContainsKey(Keyword.Token))
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
