using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord;
using Discord.WebSocket;
using KBot.Modules.Gambling.Objects;
using KBot.Services;
using Color = Discord.Color;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowService
{
    private readonly List<HighLowGame> Games = new();
    private readonly Cloudinary Cloudinary;
    private readonly DatabaseService Database;

    public HighLowService(DatabaseService database, Cloudinary cloudinary)
    {
        Database = database;
        Cloudinary = cloudinary;
    }

    public HighLowGame CreateGame(SocketUser user, IUserMessage message, int stake)
    {
        var game = new HighLowGame(Generators.GenerateID(), user, message, stake, Cloudinary, Database, Games);
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
}

public class HighLowGame : IGamblingGame
{
    public string Id { get; }
    public SocketUser User { get; }
    private IUserMessage Message { get; }
    private IGuild Guild => ((ITextChannel)Message.Channel).Guild;
    private Deck Deck { get; }
    private Card PlayerHand { get; set; }
    private Card DealerHand { get; set; }
    private int Stake { get; set; }
    private int HighStake { get; set; }
    private decimal HighMultiplier { get; set; }
    private int LowStake { get; set; }
    private decimal LowMultiplier { get; set; }
    private Cloudinary CloudinaryClient { get; }
    private DatabaseService Database { get; }
    private List<HighLowGame> Container { get; }

    public HighLowGame(string id, SocketUser user, IUserMessage message, int stake, Cloudinary cloudinary, DatabaseService databaseService, List<HighLowGame> container)
    {
        Id = id;
        User = user;
        Message = message;
        Stake = stake;
        CloudinaryClient = cloudinary;
        Database = databaseService;
        Container = container;
        Deck = new Deck();
        Draw();
    }

    public Task StartAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle("High/Low")
            .WithDescription($"Tét: **{Stake} kredit**")
            .AddField("Nagyobb", $"Szorzó: **{HighMultiplier.ToString()}**\n" +
                                 $"Nyeremény: **{HighStake.ToString()} kredit**", true)
            .AddField("Kisebb", $"Szorzó: **{LowMultiplier.ToString()}**\n" +
                                $"Nyeremény: **{LowStake.ToString()}** kredit", true)
            .WithColor(Color.Gold)
            .WithImageUrl(GetTablePicUrl())
            .Build();
        var comp = new ComponentBuilder()
            .WithButton(" ", $"highlow-high:{Id}", emote: new Emoji("⬆"))
            .WithButton(" ", $"highlow-low:{Id}", emote: new Emoji("⬇"))
            .WithButton(" ", "highlow-cancel", emote: new Emoji("❌"), disabled:true)
            .Build();
        return Message.ModifyAsync(x =>
        {
            x.Content = string.Empty;
            x.Embed = eb;
            x.Components = comp;
        });
    }

    private string Draw()
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

    public async Task GuessHigherAsync()
    {
        var result = PlayerHand.Value < DealerHand.Value;
        if (result)
        {
            Stake = HighStake;
            var imgUrl = Draw();
            var eb = new EmbedBuilder()
                .WithTitle("Higher/Lower")
                .WithDescription($"Tét: **{Stake} kredit**")
                .WithImageUrl(imgUrl)
                .WithColor(Color.Gold)
                .AddField("Nagyobb", $"Szorzó: **{HighMultiplier.ToString()}**\n" +
                                     $"Nyeremény: **{HighStake.ToString()} kredit**", true)
                .AddField("Kisebb", $"Szorzó: **{LowMultiplier.ToString()}**\n" +
                                    $"Nyeremény: **{LowStake.ToString()}** kredit", true)
                .Build();
            var components = new ComponentBuilder()
                .WithButton(" ", $"highlow-high:{Id}", emote: new Emoji("⬆"))
                .WithButton(" ", $"highlow-low:{Id}", emote: new Emoji("⬇"))
                .WithButton(" ", $"highlow-finish:{Id}", emote: new Emoji("❌"), disabled:false)
                .Build();
            await Message.ModifyAsync(x =>
            {
                x.Embed = eb;
                x.Components = components;
            }).ConfigureAwait(false);
            return;
        }

        var fEb = new EmbedBuilder()
            .WithTitle("Higher/Lower")
            .WithDescription($"Nem találtad el! Vesztettél **{Stake}** kreditet!")
            .WithColor(Color.Red)
            .WithImageUrl(GetTablePicUrl(true))
            .AddField("Nagyobb", $"Szorzó: **{HighMultiplier.ToString()}**\n" +
                                 $"Nyeremény: **{HighStake.ToString()} kredit**", true)
            .AddField("Kisebb", $"Szorzó: **{LowMultiplier.ToString()}**\n" +
                                $"Nyeremény: **{LowStake.ToString()}** kredit", true)
            .Build();
        var dbUser = await Database.GetUserAsync(Guild, User).ConfigureAwait(false);
        dbUser.GamblingProfile.HighLow.MoneyLost += Stake;
        dbUser.GamblingProfile.HighLow.Losses++;
        await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = fEb;
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }

    public async Task GuessLowerAsync()
    {
        var result = PlayerHand.Value > DealerHand.Value;
        if (result)
        {
            Stake = LowStake;
            var imgUrl = Draw();
            var eb = new EmbedBuilder()
                .WithTitle("Higher/Lower")
                .WithDescription($"Tét: **{Stake} kredit**")
                .WithColor(Color.Gold)
                .WithImageUrl(imgUrl)
                .AddField("Nagyobb", $"Szorzó: **{HighMultiplier.ToString()}**\n" +
                                     $"Nyeremény: **{HighStake.ToString()} kredit**", true)
                .AddField("Kisebb", $"Szorzó: **{LowMultiplier.ToString()}**\n" +
                                    $"Nyeremény: **{LowStake.ToString()}** kredit", true)
                .Build();
            var components = new ComponentBuilder()
                .WithButton(" ", $"highlow-high:{Id}", emote: new Emoji("⬆"))
                .WithButton(" ", $"highlow-low:{Id}", emote: new Emoji("⬇"))
                .WithButton(" ", $"highlow-finish:{Id}", emote: new Emoji("❌"), disabled:false)
                .Build();
            await Message.ModifyAsync(x =>
            {
                x.Embed = eb;
                x.Components = components;
            }).ConfigureAwait(false);
            return;
        }
        var fEb = new EmbedBuilder()
            .WithTitle("Higher/Lower")
            .WithDescription($"Nem találtad el! Vesztettél **{Stake}** kreditet!")
            .WithColor(Color.Red)
            .WithImageUrl(GetTablePicUrl(true))
            .AddField("Nagyobb", $"Szorzó: **{HighMultiplier.ToString()}**\n" +
                                 $"Nyeremény: **{HighStake.ToString()} kredit**", true)
            .AddField("Kisebb", $"Szorzó: **{LowMultiplier.ToString()}**\n" +
                                $"Nyeremény: **{LowStake.ToString()}** kredit", true)
            .Build();
        var dbUser = await Database.GetUserAsync(Guild, User).ConfigureAwait(false);
        dbUser.GamblingProfile.HighLow.MoneyLost += Stake;
        dbUser.GamblingProfile.HighLow.Losses++;
        await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = fEb;
            x.Components = new ComponentBuilder().Build(); 
        }).ConfigureAwait(false);
        Container.Remove(this);
    }

    public async Task FinishAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle("Higher/Lower")
            .WithDescription($"A játék véget ért! **{Stake}** kreditet szereztél!")
            .WithColor(Color.Green)
            .WithImageUrl(GetTablePicUrl(true))
            .AddField("Nagyobb", $"Szorzó: **{HighMultiplier.ToString()}**\n" +
                                 $"Nyeremény: **{HighStake.ToString()} kredit**", true)
            .AddField("Kisebb", $"Szorzó: **{LowMultiplier.ToString()}**\n" +
                                $"Nyeremény: **{LowStake.ToString()}** kredit", true)
            .Build();
        var dbUser = await Database.GetUserAsync(Guild, User).ConfigureAwait(false);
        dbUser.GamblingProfile.Money += Stake;
        dbUser.GamblingProfile.HighLow.MoneyWon += Stake;
        dbUser.GamblingProfile.HighLow.Wins++;
        await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = eb;
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }

    private string GetTablePicUrl(bool reveal = false)
    {
        var merged = MergePlayerAndDealer(PlayerHand.GetImage(), reveal ? DealerHand.GetImage() : (Bitmap) Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("KBot.Resources.empty.png")!));
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