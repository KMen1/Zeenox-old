using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord;
using SkiaSharp;
using Zeenox.Enums;
using Zeenox.Models.Games;
using Zeenox.Services;

namespace Zeenox.Modules.Gambling.Games;

public sealed class BlackJack : IGame
{
    public BlackJack(ulong userId, IUserMessage message, int bet, Cloudinary cloudinary)
    {
        UserId = userId;
        Message = message;
        Bet = bet;
        Cloudinary = cloudinary;
        Id = Guid.NewGuid().ToString();
        Deck = new Deck();
        Hidden = true;
        DealerCards = new Hand(Deck.DealHand());
        PlayerCards = new Hand(Deck.DealHand());
    }

    private string Id { get; }
    private IUserMessage Message { get; }
    private int Bet { get; }
    private Deck Deck { get; }
    private Hand DealerCards { get; }
    private Hand PlayerCards { get; }
    private bool Hidden { get; set; }
    private Cloudinary Cloudinary { get; }

    public ulong UserId { get; }
    public event AsyncEventHandler<GameEndEventArgs> GameEnded = null!;

    public Task StartAsync()
    {
        return UpdateMessageAsync();
    }

    public async Task HitAsync()
    {
        PlayerCards.AddCard(Deck.Draw());
        switch (PlayerCards.Value)
        {
            case > 21:
            {
                Hidden = false;
                await UpdateMessageAsync($"**Result:** You lose **{Bet:N0}** credits!")
                    .ConfigureAwait(false);
                await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose)).ConfigureAwait(false);
                return;
            }
            case 21:
            {
                Hidden = false;
                var reward = (int) (Bet * 2.5) - Bet;
                await UpdateMessageAsync($"**Result:** You win **{reward:N0}** credits!")
                    .ConfigureAwait(false);
                await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win)).ConfigureAwait(false);
                return;
            }
        }

        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task StandAsync()
    {
        Hidden = false;
        while (DealerCards.Value < 17)
            DealerCards.AddCard(Deck.Draw());
        switch (DealerCards.Value)
        {
            case > 21:
            {
                var reward = Bet * 2 - Bet;
                await UpdateMessageAsync($"**Result:** You win **{reward:N0}** credits!")
                    .ConfigureAwait(false);
                await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win)).ConfigureAwait(false);
                return;
            }
            case 21:
            {
                await UpdateMessageAsync($"**Result:** You lose **{Bet:N0}** credits!").ConfigureAwait(false);
                await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose)).ConfigureAwait(false);
                return;
            }
        }

        if (PlayerCards.Value == 21)
        {
            var reward = (int) (Bet * 2.5) - Bet;
            await UpdateMessageAsync($"**Result:** You win **{reward:N0}** credits!")
                .ConfigureAwait(false);
            await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win)).ConfigureAwait(false);
            return;
        }

        if (PlayerCards.Value > DealerCards.Value)
        {
            var reward = Bet * 2 - Bet;
            await UpdateMessageAsync($"**Result:** You win **{reward:N0}** credits!")
                .ConfigureAwait(false);
            await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win)).ConfigureAwait(false);
            return;
        }

        if (PlayerCards.Value < DealerCards.Value)
        {
            await UpdateMessageAsync($"**Result:** You lose **{Bet:N0}** credits!")
                .ConfigureAwait(false);
            await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose)).ConfigureAwait(false);
            return;
        }

        await UpdateMessageAsync("**Result:** Tie - You get your bet back!").ConfigureAwait(false);
        await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, 0, GameResult.Tie)).ConfigureAwait(false);
    }

    private Task UpdateMessageAsync(string? desc = null)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Blackjack")
            .WithColor(Color.Gold)
            .WithDescription($"**Bet:** {Bet:N0} credits" + (desc is null ? "" : $"\n{desc}"))
            .WithImageUrl(GetImageUrl())
            .AddField($"Player - {PlayerCards.Value.ToString()}", "\u200b", true)
            .AddField($"Dealer - {(Hidden ? "?" : DealerCards.Value.ToString())}", "\u200b", true);

        if (desc is null)
            return Message.ModifyAsync(
                x =>
                {
                    x.Embed = embedBuilder.Build();
                    x.Components = new ComponentBuilder()
                        .WithButton("Hit", "blackjack-hit", ButtonStyle.Success)
                        .WithButton("Stand", "blackjack-stand", ButtonStyle.Danger)
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
        var upParams = new ImageUploadParams
        {
            File = new FileDescription($"blackjack-{Id}.png", CreateImage()),
            PublicId = $"blackjack-{Id}"
        };
        return Cloudinary.Upload(upParams).Url.ToString();
    }

    private Stream CreateImage()
    {
        var playerImages = PlayerCards.GetBitmaps();
        var dealerImages = Hidden
            ? new[]
            {
                DealerCards.GetBitmap(0),
                SKBitmap.Decode(
                    File.Open("Resources/gambling/empty.png", FileMode.Open, FileAccess.Read)
                )
            }
            : DealerCards.GetBitmaps();
        var height = playerImages.Max(x => x.Height);

        using var surface = SKSurface.Create(new SKImageInfo(400, height));
        using var canvas = surface.Canvas;
        var localWidth = 0;
        foreach (var image in playerImages)
        {
            canvas.DrawBitmap(image, localWidth, 0);
            localWidth += 15;
        }

        localWidth = 188;
        foreach (var image in dealerImages)
        {
            canvas.DrawBitmap(image, localWidth, 0);
            localWidth += 15;
        }

        return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }

    private Task OnGameEndedAsync(GameEndEventArgs e)
    {
        return GameEnded.Invoke(this, e);
    }
}