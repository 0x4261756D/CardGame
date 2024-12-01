using System.Collections.Generic;
using CardGameUtils.GameEnumsAndStructs;

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
		if(card is Creature creature && creature.Keywords.ContainsKey(Keyword.Token))
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
