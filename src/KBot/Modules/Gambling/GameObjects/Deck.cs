using System;
using System.Collections.Generic;
using KBot.Enums;

namespace KBot.Modules.Gambling.Objects;

public class Deck
{
    public List<Card> Cards { get; }
    public Deck()
    {
        Cards = new List<Card>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Face face in Enum.GetValues(typeof(Face)))
            {
                Cards.Add(new Card(suit, face));
            }
        }
        var rnd = new Random();
        for (var i = 0; i < Cards.Count; i++)
        {
            var r = rnd.Next(i, Cards.Count);
            (Cards[i], Cards[r]) = (Cards[r], Cards[i]);
        }
    }

    public Card Draw()
    {
        var card = Cards[0];
        Cards.Remove(card);
        return card;
    }

    public List<Card> DealHand()
    {
        var hand = new List<Card>
        {
            Cards[0],
            Cards[1]
        };
        Cards.RemoveRange(0, 2);
        return hand;
    }
}