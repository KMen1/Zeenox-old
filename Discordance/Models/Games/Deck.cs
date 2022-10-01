using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Discordance.Models.Games;

public class Deck
{
    public List<Card> Cards { get; }

    public Deck()
    {
        Cards = new List<Card>();
        AddCards();
        Shuffle();
    }

    private void AddCards()
    {
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Face face in Enum.GetValues(typeof(Face)))
            {
                Cards.Add(new Card(suit, face));
            }
        }
    }

    private void Shuffle()
    {
        for (var i = 0; i < Cards.Count; i++)
        {
            var r = RandomNumberGenerator.GetInt32(i, Cards.Count);
            (Cards[i], Cards[r]) = (Cards[r], Cards[i]);
        }
    }

    public Card Draw()
    {
        var card = Cards[0];
        Cards.RemoveAt(0);
        return card;
    }

    public List<Card> DealHand()
    {
        var hand = new List<Card> { Cards[0], Cards[1] };
        Cards.RemoveRange(0, 2);
        return hand;
    }
}
