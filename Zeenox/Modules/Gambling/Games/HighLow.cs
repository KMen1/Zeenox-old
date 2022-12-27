using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord;
using SkiaSharp;
using Zeenox.Enums;
using Zeenox.Extensions;
using Zeenox.Models;
using Zeenox.Models.Games;
using Zeenox.Services;

namespace Zeenox.Modules.Gambling.Games;

public sealed class HighLow : IGame
{
    public HighLow(ulong userId, IUserMessage message, int bet, Cloudinary cloudinary)
    {
        UserId = userId;
        Message = message;
        Stake = bet;
        Bet = bet;
        CloudinaryClient = cloudinary;
        Hidden = true;
        Deck = new Deck();
    }

    private IUserMessage Message { get; }
    private Deck Deck { get; set; }
    private Card PlayerHand { get; set; } = null!;
    private Card DealerHand { get; set; } = null!;
    private int Stake { get; set; }
    private int Bet { get; }
    private decimal HighMultiplier { get; set; }
    private decimal LowMultiplier { get; set; }
    private int HighStake => (int) (Stake * HighMultiplier);
    private int LowStake => (int) (Stake * LowMultiplier);
    private bool Hidden { get; set; }
    private Cloudinary CloudinaryClient { get; }

    public ulong UserId { get; }
    public event AsyncEventHandler<GameEndEventArgs> GameEnded = null!;

    public Task StartAsync()
    {
        Draw();
        return UpdateMessageAsync();
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

        CalculateMultiplier();
    }

    private void CalculateMultiplier()
    {
        var cardsCount = Deck.Cards.Count;
        var lowerCardsCount = Deck.Cards.Count(x => x.Value < PlayerHand.Value);
        var higherCardsCount = Deck.Cards.Count(x => x.Value > PlayerHand.Value);
        HighMultiplier = Math.Round((decimal) cardsCount / higherCardsCount, 2);
        LowMultiplier = Math.Round((decimal) cardsCount / lowerCardsCount, 2);
    }

    public async Task HigherAsync()
    {
        if (PlayerHand.Value < DealerHand.Value)
        {
            Stake = HighStake;
            Draw();
            await UpdateMessageAsync().ConfigureAwait(false);
            return;
        }

        Hidden = false;
        await UpdateMessageAsync($"**Result:** You lost **{Bet:N0}** credits!")
            .ConfigureAwait(false);
        await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose)).ConfigureAwait(false);
    }

    public async Task LowerAsync()
    {
        if (PlayerHand.Value > DealerHand.Value)
        {
            Stake = LowStake;
            Draw();
            await UpdateMessageAsync().ConfigureAwait(false);
            return;
        }

        Hidden = false;
        await UpdateMessageAsync($"**Result:** You lost **{Bet:N0}** credits!")
            .ConfigureAwait(false);
        await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose)).ConfigureAwait(false);
    }

    public async Task FinishAsync()
    {
        Hidden = false;
        await UpdateMessageAsync($"**Result:** You win **{Stake:N0}** credits!").ConfigureAwait(false);
        await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, Stake, GameResult.Win)).ConfigureAwait(false);
    }

    private Task UpdateMessageAsync(string? desc = null)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Higher/Lower")
            .WithDescription(
                $"**Original Bet:** {Bet:N0} credits\n**Current Bet:** {Stake:N0} credits"
                + (desc is null ? "" : $"\n{desc}")
            )
            .WithColor(Color.Gold)
            .WithImageUrl(GetImageUrl())
            .AddField(
                $"Higher - {HighMultiplier:0.00}x",
                $"Prize: **{HighStake:N0} credits**",
                true
            )
            .AddField($"Lower - {LowMultiplier:0.00}x", $"Prize: **{LowStake:N0} credits**", true);

        if (desc is null)
            return Message.ModifyAsync(
                x =>
                {
                    x.Embed = embedBuilder.Build();
                    x.Components = new ComponentBuilder()
                        .WithButton(" ", "highlow-high", emote: new Emoji("⬆"))
                        .WithButton(" ", "highlow-low", emote: new Emoji("⬇"))
                        .WithButton(" ", "highlow-finish", emote: new Emoji("❌"))
                        .Build();
                }
            );

        return Message.ModifyAsync(
            x =>
            {
                x.Embed = embedBuilder.Build();
                x.Components = new ComponentBuilder().Build();
            }
        );
    }

    private string GetImageUrl()
    {
        var stream = MergePlayerAndDealer(
            PlayerHand.GetImage(),
            Hidden
                ? SKBitmap.Decode(
                    File.Open("Resources/gambling/empty.png", FileMode.Open, FileAccess.Read)
                )
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

    private static Stream MergePlayerAndDealer(SKBitmap player, SKBitmap dealer)
    {
        var height = player.Height > dealer.Height ? player.Height : dealer.Height;

        using var surface = SKSurface.Create(new SKImageInfo(200, height));
        using var canvas = surface.Canvas;
        canvas.DrawBitmap(player, 0, 0);
        canvas.DrawBitmap(dealer, 120, 0);
        return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }

    private Task OnGameEndedAsync(GameEndEventArgs e)
    {
        return GameEnded.Invoke(this, e);
    }
}