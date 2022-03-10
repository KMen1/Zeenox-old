using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackService
{
    private readonly List<BlackJackGame> Games = new();

    public BlackJackGame CreateGame(SocketUser user, int stake, Cloudinary cloudinary)
    {
        var game = new BlackJackGame(Games, CreateId(), user, stake, cloudinary);
        Games.Add(game);
        return game;
    }
    public BlackJackGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }
    public void RemoveGame(string id)
    {
        Games.Remove(GetGame(id));
    }
    public void RemoveGame(BlackJackGame game)
    {
        Games.Remove(game);
    }
    private static string CreateId()
    {
        var ticks = new DateTime(2016, 1, 1).Ticks;
        var ans = DateTime.Now.Ticks - ticks;
        return ans.ToString("x");
    }
}

public class BlackJackGame
{
    public string Id { get; }
    private Deck Deck { get; }
    public SocketUser Player { get; }
    private List<Card> DealerCards { get; }
    private List<Card> PlayerCards { get; }
    public int Stake { get; private set; }
    private Cloudinary CloudinaryClient { get; }
    public GameState State { get; private set; }

    private List<BlackJackGame> Container { get; }
    public bool Hidden = true;

    public BlackJackGame(List<BlackJackGame> container, string id, SocketUser player, int stake, Cloudinary cloudinary)
    {
        Container = container;
        Id = id;
        Deck = new Deck();
        Player = player;
        Stake = stake;
        CloudinaryClient = cloudinary;
        DealerCards = Deck.DealHand();
        PlayerCards = Deck.DealHand();
    }

    public void HitPlayer()
    {
        PlayerCards.Add(Deck.Draw());
        var playerValue = GetCardsValue(PlayerCards);
        switch (playerValue)
        {
            case > 21:
                State = GameState.PlayerBust;
                Hidden = false;
                Container.Remove(this);
                return;
            case 21:
                State = GameState.PlayerBlackjack;
                Hidden = false;
                Stake = (int)(Stake * 2.5);
                Container.Remove(this);
                return;
        }
        State = GameState.Running;
    }

    public void StandPlayer()
    {
        Hidden = false;
        var dealerValue = GetCardsValue(DealerCards);
        var playerValue = GetCardsValue(PlayerCards);
        while (dealerValue < 17)
        {
            dealerValue = HitDealer();
        }
        switch (dealerValue)
        {
            case > 21:
                State = GameState.DealerBust;
                Stake *= 2;
                Container.Remove(this);
                return;
            case 21:
                State = GameState.DealerBlackjack;
                Container.Remove(this);
                return;
        }
        if (playerValue > dealerValue)
        {
            State = GameState.PlayerWon;
            Stake *= 2;
            Container.Remove(this);
            return;
        }
        if (playerValue < dealerValue)
        {
            State = GameState.DealerWon;
            Container.Remove(this);
            return;
        }
        State = GameState.Push;
        Container.Remove(this);
    }

    private int HitDealer()
    {
        DealerCards.Add(Deck.Draw());
        return GetCardsValue(DealerCards);
    }

    public int GetPlayerSum()
    {
        return GetCardsValue(PlayerCards);
    }

    public int GetDealerSum()
    {
        return GetCardsValue(DealerCards);
    }

    public GameState GetState()
    {
        return State;
    }

    public string GetTablePicUrl()
    {
        List<Bitmap> dealerImages = new();
        if (Hidden)
        {
            dealerImages.Add(DealerCards[0].GetImage());
            dealerImages.Add((Bitmap)Image.FromFile("empty.png"));
        }
        else
        {
            dealerImages = DealerCards.ConvertAll(card => card.GetImage());
        }
        var playerImages = PlayerCards.ConvertAll(card => card.GetImage());
        var pMerged = MergeImages(playerImages);
        var dMerged = MergeImages(dealerImages);
        var merged = MergePlayerAndDealer(pMerged, dMerged);
        var stream = new MemoryStream();
        merged.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        var upParams = new ImageUploadParams
        {
            File = new FileDescription($"blackjack-{Id}.png", stream),
            PublicId = $"blackjack-{Id}"
        };
        var result = CloudinaryClient.Upload(upParams);
        return result.Url.ToString();
    }

    private static int GetCardsValue(List<Card> cards)
    {
        var value = 0;
        var aces = 0;
        foreach (var card in cards)
        {
            if (card.Face is Face.Ace)
            {
                aces++;
                continue;
            }

            value += card.Value;
        }

        for (var i = 0; i < aces; i++)
        {
            if (value + 11 <= 21)
            {
                value += 11;
            }
            else
            {
                value++;
            }
        }

        return value;
    }

    private static Bitmap MergeImages(IEnumerable<Bitmap> images)
    {
        var enumerable = images as IList<Bitmap> ?? images.ToList();

        var width = 0;
        var height = 0;

        foreach (var image in enumerable)
        {
            width += image.Width;
            height = image.Height > height
                ? image.Height
                : height;
        }

        var bitmap = new Bitmap(width - enumerable[0].Width + 21, height);
        using var g = Graphics.FromImage(bitmap);
        var localWidth = 0;
        foreach (var image in enumerable)
        {
            g.DrawImage(image, localWidth, 0);
            localWidth += 15;
        }

        return bitmap;
    }

    private static Bitmap MergePlayerAndDealer(Image player, Image dealer)
    {
        var height = player.Height > dealer.Height
            ? player.Height
            : dealer.Height;

        var bitmap = new Bitmap(360, height);
        using var g = Graphics.FromImage(bitmap);
        g.DrawImage(player, 0, 0);
        g.DrawImage(dealer, 188, 0);
        return bitmap;
    }
}

public class Card
{
    public Suit Suit { get; }
    public Face Face { get; }
    public int Value { get; }
    public Card(Suit suit, Face face)
    {
        Suit = suit;
        Face = face;

        Value = face switch {
            Face.Jack => 10,
            Face.Queen => 10,
            Face.King => 10,
            _ => Value = (int)face + 1
        };
    }

    public Bitmap GetImage()
    {
        var y = 0;
        const int height = 97;
        const int width = 73;

        y = Suit switch
        {
            Suit.Hearts => 196,
            Suit.Spades => 98,
            Suit.Clubs => 0,
            Suit.Diamonds => 294,
            _ => y
        };

        var x = width * (Value - 1);
        var source = (Bitmap)Image.FromFile("cards.png");
        var img = new Bitmap(width, height);
        using var g = Graphics.FromImage(img);
        g.DrawImage(source, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
        return img;
    }
}
public class Deck
{
    public List<Card> Cards { get; }
    public int MaxValue { get; }
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
        MaxValue = Cards.Sum(c => c.Value);
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

public enum Suit
{
    Clubs,
    Spades,
    Diamonds,
    Hearts
}
public enum Face
{
    Ace,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King
}

public enum GameState
{
    Running,
    PlayerBust,
    DealerBust,
    PlayerBlackjack,
    DealerBlackjack,
    PlayerWon,
    DealerWon,
    Push
}