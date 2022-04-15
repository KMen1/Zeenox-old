using System.Drawing;
using System.Reflection;
using KBot.Enums;

namespace KBot.Modules.Gambling.Objects;

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
            _ => Value = (int) face + 1
        };
    }

    private Suit Suit { get; }
    public Face Face { get; }
    public int Value { get; }

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
        var source =
            Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("KBot.Resources.cards.png")!);
        var img = new Bitmap(width, height);
        using var g = Graphics.FromImage(img);
        g.DrawImage(source, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
        return img;
    }
}