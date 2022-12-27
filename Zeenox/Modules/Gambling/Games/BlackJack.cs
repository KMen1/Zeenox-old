using System;
using System.Collections.Generic;
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
        DealerCards = Deck.DealHand();
        PlayerCards = Deck.DealHand();
    }

    private string Id { get; }
    private IUserMessage Message { get; }
    private int Bet { get; }
    private Deck Deck { get; }
    private List<Card> DealerCards { get; }
    private int DealerScore => DealerCards.GetValue();
    private List<Card> PlayerCards { get; }
    private int PlayerScore => PlayerCards.GetValue();
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
        PlayerCards.Add(Deck.Draw());
        switch (PlayerScore)
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
        while (DealerScore < 17)
            DealerCards.Add(Deck.Draw());
        switch (DealerScore)
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

        if (PlayerScore == 21)
        {
            var reward = (int) (Bet * 2.5) - Bet;
            await UpdateMessageAsync($"**Result:** You win **{reward:N0}** credits!")
                .ConfigureAwait(false);
            await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win)).ConfigureAwait(false);
            return;
        }

        if (PlayerScore > DealerScore)
        {
            var reward = Bet * 2 - Bet;
            await UpdateMessageAsync($"**Result:** You win **{reward:N0}** credits!")
                .ConfigureAwait(false);
            await OnGameEndedAsync(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win)).ConfigureAwait(false);
            return;
        }

        if (PlayerScore < DealerScore)
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
            .AddField($"Player - {PlayerScore.ToString()}", "\u200b", true)
            .AddField($"Dealer - {(Hidden ? "?" : DealerScore.ToString())}", "\u200b", true);

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
        var playerImages = PlayerCards.ConvertAll(card => card.GetImage());
        var dealerImages = Hidden
            ? new List<SKBitmap>
            {
                DealerCards[0].GetImage(),
                SKBitmap.Decode(
                    File.Open("Resources/gambling/empty.png", FileMode.Open, FileAccess.Read)
                )
            }
            : DealerCards.ConvertAll(card => card.GetImage());
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