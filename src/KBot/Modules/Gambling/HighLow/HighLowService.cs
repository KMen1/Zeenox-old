using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
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
        var game = new HighLowGame(Guid.NewGuid().ToString().Split("-")[0], user, message, stake, Cloudinary, Database, Games);
        Games.Add(game);
        return game;
    }
    public HighLowGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
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
    public int Stake { get; private set; }
    public int HighStake { get; private set; }
    public decimal HighMultiplier { get; private set; }
    public int LowStake { get; private set; }
    public decimal LowMultiplier { get; private set; }
    private bool Hidden { get; set; }
    private Cloudinary CloudinaryClient { get; }
    private DatabaseService Database { get; }
    private List<HighLowGame> Container { get; }

    public HighLowGame(
        string id,
        SocketUser user,
        IUserMessage message,
        int stake,
        Cloudinary cloudinary,
        DatabaseService databaseService,
        List<HighLowGame> container)
    {
        Id = id;
        User = user;
        Message = message;
        Stake = stake;
        CloudinaryClient = cloudinary;
        Database = databaseService;
        Container = container;
        Hidden = false;
        Deck = new Deck();
    }

    public Task StartAsync()
    {
        Draw();
        return Message.ModifyAsync(x =>
        {
            x.Content = string.Empty;
            x.Embed = new EmbedBuilder().HighLowEmbed(this);
            x.Components = new ComponentBuilder()
                .WithButton(" ", $"highlow-high:{Id}", emote: new Emoji("⬆"))
                .WithButton(" ", $"highlow-low:{Id}", emote: new Emoji("⬇"))
                .WithButton(" ", "highlow-cancel", emote: new Emoji("❌"))
                .Build();
        });
    }

    private void Draw()
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
        while (lowerCards == 0 || higherCards == 0)
        {
            DealerHand = Deck.Draw();
            lowerCards = Deck.Cards.Count(x => x.Value < PlayerHand.Value);
            higherCards = Deck.Cards.Count(x => x.Value > PlayerHand.Value);
        }
        HighMultiplier = Math.Round((decimal)cards / higherCards, 2);
        HighStake = (int)(Stake * HighMultiplier);
        LowMultiplier = Math.Round((decimal)cards / lowerCards, 2);
        LowStake = (int)(Stake * LowMultiplier);
    }

    public async Task GuessHigherAsync()
    {
        var result = PlayerHand.Value < DealerHand.Value;
        if (result)
        {
            Stake = HighStake;
            Draw();
            await Message.ModifyAsync(x => x.Embed = new EmbedBuilder().HighLowEmbed(this)).ConfigureAwait(false);
            return;
        }
        Hidden = false;
        await Database.UpdateUserAsync(Guild, User, x =>
        {
            x.Gambling.MoneyLost += Stake;
            x.Gambling.Losses++;
        }).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().HighLowEmbed(this, $"Nem találtad el! Vesztettél **{Stake}** kreditet!", Color.Red);
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
            Draw();
            await Message.ModifyAsync(x => x.Embed = new EmbedBuilder().HighLowEmbed(this)).ConfigureAwait(false);
            return;
        }
        Hidden = false;
        await Database.UpdateUserAsync(Guild, User, x =>
        {
            x.Gambling.MoneyLost += Stake;
            x.Gambling.Losses++;
        }).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().HighLowEmbed(this, $"Nem találtad el! Vesztettél **{Stake}** kreditet!", Color.Red);
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }

    public async Task FinishAsync()
    {
        Hidden = false;
        await Database.UpdateUserAsync(Guild, User, x =>
        {
            x.Gambling.Balance += Stake;
            x.Gambling.MoneyWon += Stake;
            x.Gambling.Wins++;
            x.Transactions.Add(new Transaction(Id, TransactionType.Gambling, Stake, "HL - WIN"));
        }).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().HighLowEmbed(this, $"A játék véget ért! **{Stake}** kreditet szereztél!", Color.Green);
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }

    public string GetTablePicUrl()
    {
        var merged = MergePlayerAndDealer(PlayerHand.GetImage(), 
            Hidden ?
                (Bitmap)Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("KBot.Resources.empty.png")!) :
                DealerHand.GetImage());
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