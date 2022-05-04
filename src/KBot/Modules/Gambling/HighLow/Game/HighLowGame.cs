using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord;
using Discord.WebSocket;
using KBot.Extensions;
using KBot.Modules.Gambling.GameObjects;
using KBot.Modules.Gambling.Generic;
using Color = Discord.Color;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace KBot.Modules.Gambling.HighLow.Game;

public sealed class HighLowGame : IGame
{
    public HighLowGame(SocketGuildUser user, IUserMessage message, int stake, Cloudinary cloudinary)
    {
        Id = Guid.NewGuid().ToShortId();
        User = user;
        Message = message;
        Stake = stake;
        Bet = stake;
        CloudinaryClient = cloudinary;
        Hidden = true;
        Deck = new Deck();
    }

    public string Id { get; }
    public SocketGuildUser User { get; }
    private IUserMessage Message { get; }
    private Deck Deck { get; set; }
    public int RemainCards => Deck.Cards.Count;
    private Card PlayerHand { get; set; } = null!;
    private Card DealerHand { get; set; } = null!;
    public int Stake { get; private set; }
    public int Bet { get; }
    public int HighStake { get; private set; }
    public decimal HighMultiplier { get; private set; }
    public int LowStake { get; private set; }
    public decimal LowMultiplier { get; private set; }
    private bool Hidden { get; set; }
    private Cloudinary CloudinaryClient { get; }
    public event EventHandler<GameEndedEventArgs>? GameEnded;

    public Task StartAsync()
    {
        Draw();
        return Message.ModifyAsync(
            x =>
            {
                x.Content = string.Empty;
                x.Embed = new HighLowEmbedBuilder(this).Build();
                x.Components = new ComponentBuilder()
                    .WithButton(" ", $"highlow-high:{Id}", emote: new Emoji("⬆"))
                    .WithButton(" ", $"highlow-low:{Id}", emote: new Emoji("⬇"))
                    .WithButton(" ", $"highlow-finish:{Id}", emote: new Emoji("❌"))
                    .Build();
            }
        );
    }

    private void Draw()
    {
        if (Deck.Cards.Count == 0)
            Deck = new Deck();
        PlayerHand = Deck.Draw();
        DealerHand = Deck.Draw();
        while (PlayerHand.Value is 10 or 1 || PlayerHand.Value == DealerHand.Value)
        {
            if (Deck.Cards.Count == 0)
                Deck = new Deck();
            PlayerHand = Deck.Draw();
        }

        var cards = Deck.Cards.Count;
        var lowerCards = Deck.Cards.Count(x => x.Value < PlayerHand.Value);
        var higherCards = Deck.Cards.Count(x => x.Value > PlayerHand.Value);
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
            await Message
                .ModifyAsync(x => x.Embed = new HighLowEmbedBuilder(this).Build())
                .ConfigureAwait(false);
            return;
        }

        Hidden = false;
        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new HighLowEmbedBuilder(
                        this,
                        $"**Result:** You lost **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                    )
                        .WithColor(Color.Red)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "HighLow: LOSE", false));
    }

    public async Task GuessLowerAsync()
    {
        var result = PlayerHand.Value > DealerHand.Value;
        if (result)
        {
            Stake = LowStake;
            Draw();
            await Message
                .ModifyAsync(x => x.Embed = new HighLowEmbedBuilder(this).Build())
                .ConfigureAwait(false);
            return;
        }

        Hidden = false;
        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new HighLowEmbedBuilder(
                        this,
                        $"**Result:** You lost **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                    )
                        .WithColor(Color.Red)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "HighLow: LOSE", false));
    }

    public async Task FinishAsync()
    {
        Hidden = false;
        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new HighLowEmbedBuilder(
                        this,
                        $"**Result:** You win **{Stake.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                    )
                        .WithColor(Color.Green)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, Stake, "HighLow: WIN", false));
    }

    public string GetTablePicUrl()
    {
        var merged = MergePlayerAndDealer(
            PlayerHand.GetImage(),
            Hidden
              ? (Bitmap)Image.FromStream(
                    Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream("KBot.Resources.empty.png")!
                )
              : DealerHand.GetImage()
        );
        var stream = new MemoryStream();
        merged.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        var upParams = new ImageUploadParams
        {
            File = new FileDescription($"highlow-{Id}.png", stream),
            PublicId = $"highlow-{Id}"
        };
        var result = CloudinaryClient.Upload(upParams);
        return result.Url.ToString();
    }

    private static Bitmap MergePlayerAndDealer(Image player, Image dealer)
    {
        var height = player.Height > dealer.Height ? player.Height : dealer.Height;

        var bitmap = new Bitmap(200, height);
        using var g = Graphics.FromImage(bitmap);
        g.DrawImage(player, 0, 0);
        g.DrawImage(dealer, 120, 0);
        return bitmap;
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}
