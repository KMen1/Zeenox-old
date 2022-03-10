using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord.WebSocket;
using KBot.Modules.Gambling.BlackJack;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowService
{
    private readonly List<HighLowGame> Games = new();

    public HighLowGame CreateGame(SocketUser user, int stake, Cloudinary cloudinary)
    {
        var game = new HighLowGame(CreateId(), user, stake, cloudinary);
        Games.Add(game);
        return game;
    }
    public HighLowGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }
    public void RemoveGame(string id)
    {
        Games.RemoveAll(x => x.Id == id);
    }
    private static string CreateId()
    {
        var ticks = new DateTime(2016, 1, 1).Ticks;
        var ans = DateTime.Now.Ticks - ticks;
        return ans.ToString("x");
    }
}

public class HighLowGame
{
    public string Id { get; }
    public SocketUser Player { get; }
    private Deck Deck { get; }
    private Card PlayerHand { get; set; }
    private Card DealerHand { get; set; }
    public int Stake { get; private set; }
    public int HighStake { get; private set; }
    public decimal HighMultiplier { get; private set; }
    public int LowStake { get; private set; }
    public decimal LowMultiplier { get; private set; }
    private Cloudinary CloudinaryClient { get; }
    private List<HighLowGame> Container { get; }

    public HighLowGame(string id, SocketUser user, int stake, Cloudinary cloudinary)
    {
        Id = id;
        Player = user;
        Stake = stake;
        CloudinaryClient = cloudinary;
        Deck = new Deck();
        Draw();
    }

    public string Draw()
    {
        PlayerHand = Deck.Draw();
        DealerHand = Deck.Draw();
        while (PlayerHand.Value == DealerHand.Value)
        {
            DealerHand = Deck.Draw();
        }
        var cards = Deck.Cards.Count;
        var lowerCards = Deck.Cards.Count(x => x.Value < PlayerHand.Value);
        var higherCards = Deck.Cards.Count(x => x.Value > PlayerHand.Value);
        if (higherCards == 0)
        {
            HighMultiplier = 0;
            HighStake = 0;
            LowMultiplier = 1;
            LowStake = Stake;
        }
        else if (lowerCards == 0)
        {
            LowMultiplier = 0;
            LowStake = 0;
            HighMultiplier = 1;
            HighStake = Stake;
        }
        else
        {
            HighMultiplier = Math.Round((decimal)cards / higherCards, 2);
            HighStake = (int)(Stake * HighMultiplier);
            LowMultiplier = Math.Round((decimal)cards / lowerCards, 2);
            LowStake = (int)(Stake * LowMultiplier);
        }
        return GetTablePicUrl();
    }
    public string Reveal()
    {
        return GetTablePicUrl(true);
    }

    public bool High()
    {
        var result = PlayerHand.Value < DealerHand.Value;
        if (result)
            Stake = HighStake;
        return result;
    }

    public bool Low()
    {
        var result = PlayerHand.Value > DealerHand.Value;
        if (result)
            Stake = LowStake;
        return result;
    }

    public string GetTablePicUrl(bool reveal = false)
    {
        var merged = MergePlayerAndDealer(PlayerHand.GetImage(), reveal ? DealerHand.GetImage() : Image.FromFile("empty.png"));
        var stream = new MemoryStream();
        merged.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        var upParams = new ImageUploadParams()
        {
            File = new FileDescription($"highlow-{Id}.png", stream),
            PublicId = $"highlow-{Id}"
        };
        var result = CloudinaryClient.Upload(upParams);
        return result.Url.ToString();
    }

    private static Bitmap MergePlayerAndDealer(Image player, Image dealer)
    {
        var height = player.Height > dealer.Height
            ? player.Height
            : dealer.Height;

        var bitmap = new Bitmap(165, height);
        using var g = Graphics.FromImage(bitmap);
        g.DrawImage(player, 0, 0);
        g.DrawImage(dealer, 90, 0);
        return bitmap;
    }
}