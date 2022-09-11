using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Models;
using Discordance.Models.Games;
using SkiaSharp;
using Color = Discord.Color;

namespace Discordance.Modules.Gambling.HighLow;

public sealed class HighLowGame : IGame
{
    public HighLowGame(ulong userId, IUserMessage message, int stake, Cloudinary cloudinary)
    {
        UserId = userId;
        Message = message;
        Stake = stake;
        Bet = stake;
        CloudinaryClient = cloudinary;
        Hidden = true;
        Deck = new Deck();
    }

    public ulong UserId { get; }
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
    public event EventHandler<GameEndEventArgs>? GameEnded;

    public Task StartAsync()
    {
        Draw();
        return Message.ModifyAsync(
            x =>
            {
                x.Content = string.Empty;
                x.Embed = new HighLowEmbedBuilder(this).Build();
                x.Components = new ComponentBuilder()
                    .WithButton(" ", "highlow-high", emote: new Emoji("⬆"))
                    .WithButton(" ", "highlow-low", emote: new Emoji("⬇"))
                    .WithButton(" ", "highlow-finish", emote: new Emoji("❌"))
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
        OnGameEnded(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose));
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
        OnGameEnded(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose));
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
        OnGameEnded(new GameEndEventArgs(UserId, Bet, Stake, GameResult.Win));
    }

    public string GetTablePicUrl()
    {
        var stream = MergePlayerAndDealer(
            PlayerHand.GetImage(),
            Hidden
              ? Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("KBot.Resources.gambling.empty.png")!
              : DealerHand.GetImage()
        );
        var id = Guid.NewGuid().ToShortId();
        var upParams = new ImageUploadParams
        {
            File = new FileDescription($"highlow-{id}.png", stream),
            PublicId = $"highlow-{id}"
        };
        var result = CloudinaryClient.Upload(upParams);
        return result.Url.ToString();
    }

    private static Stream MergePlayerAndDealer(Stream player, Stream dealer)
    {
        var playerBitmap = SKBitmap.Decode(player);
        var dealerBitmap = SKBitmap.Decode(dealer);
        var height =
            playerBitmap.Height > dealerBitmap.Height ? playerBitmap.Height : dealerBitmap.Height;

        using var surface = SKSurface.Create(new SKImageInfo(200, height));
        using var canvas = surface.Canvas;
        canvas.DrawBitmap(playerBitmap, 0, 0);
        canvas.DrawBitmap(dealerBitmap, 120, 0);
        return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }

    private void OnGameEnded(GameEndEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}
