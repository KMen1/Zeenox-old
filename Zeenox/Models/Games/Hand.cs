using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Zeenox.Models.Games;

public class Hand
{
    public Hand(IEnumerable<Card> cards)
    {
        Cards = cards.ToList();
    }

    private List<Card> Cards { get; }

    public int Value
    {
        get
        {
            var aces = Cards.Count(x => x.Face is Face.Ace);
            var value = Cards.Sum(card => card.Value);

            for (var i = 0; i < aces; i++)
                if (value + 11 <= 21)
                    value += 11;
                else
                    value++;

            return value;
        }
    }

    public SKBitmap[] GetBitmaps()
    {
        return Cards.Select(c => c.GetImage()).ToArray();
    }

    public SKBitmap GetBitmap(int index)
    {
        return Cards[index].GetImage();
    }

    public void AddCard(Card card)
    {
        Cards.Add(card);
    }
}