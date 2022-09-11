using System.IO;
using System.Reflection;
using SkiaSharp;

namespace Discordance.Models.Games;

public class Card
{
    public Card(Suit suit, Face face)
    {
        Suit = suit;
        Face = face;

        Value = face switch
        {
            Face.Jack => 10,
            Face.Queen => 10,
            Face.King => 10,
            _ => (int)face + 1
        };
    }

    private Suit Suit { get; }
    public Face Face { get; }
    public int Value { get; }

    public Stream GetImage()
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

        var x = Face switch
        {
            Face.Queen => width * (int)Face,
            Face.King => width * (int)Face,
            Face.Jack => width * (int)Face,
            _ => width * (Value - 1)
        };

        using var source = SKBitmap.Decode(
            Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("KBot.Resources.gambling.cards.png")!
        );
        using var image = SKImage.FromBitmap(source);
        using var subset = image.Subset(SKRectI.Create(x, y, width, height));
        return subset.Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }
}
