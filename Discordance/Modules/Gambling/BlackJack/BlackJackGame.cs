using System;
using System.Collections.Generic;
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
using Face = Discordance.Models.Games.Face;

namespace Discordance.Modules.Gambling.BlackJack;

public sealed class BlackJackGame : IGame
{
    public BlackJackGame(ulong userId, IUserMessage message, int bet, Cloudinary cloudinary)
    {
        Message = message;
        Deck = new Deck();
        UserId = userId;
        Bet = bet;
        Hidden = true;
        CloudinaryClient = cloudinary;
        DealerCards = Deck.DealHand();
        PlayerCards = Deck.DealHand();
    }

    private Deck Deck { get; }
    public ulong UserId { get; }
    private IUserMessage Message { get; }
    private List<Card> DealerCards { get; }

    public int DealerScore => GetCardsValue(DealerCards);
    private List<Card> PlayerCards { get; }
    public int PlayerScore => GetCardsValue(PlayerCards);
    public int Bet { get; }
    public bool Hidden { get; private set; }
    private Cloudinary CloudinaryClient { get; }
    public event EventHandler<GameEndEventArgs>? GameEnded;

    public Task StartAsync()
    {
        return Message.ModifyAsync(
            x =>
            {
                x.Content = string.Empty;
                x.Embed = new BlackJackEmbedBuilder(this).Build();
                x.Components = new ComponentBuilder()
                    .WithButton("Hit", "blackjack-hit")
                    .WithButton("Stand", "blackjack-stand")
                    .Build();
            }
        );
    }

    public async Task HitAsync()
    {
        PlayerCards.Add(Deck.Draw());
        switch (PlayerScore)
        {
            case > 21:
            {
                Hidden = false;
                await Message
                    .ModifyAsync(
                        x =>
                        {
                            x.Embed = new BlackJackEmbedBuilder(
                                this,
                                $"**Result:** You lose **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                            )
                                .WithColor(Color.Red)
                                .Build();
                            x.Components = new ComponentBuilder().Build();
                        }
                    )
                    .ConfigureAwait(false);
                OnGameEnded(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose));
                return;
            }
            case 21:
            {
                Hidden = false;
                var reward = (int)(Bet * 2.5) - Bet;
                await Message
                    .ModifyAsync(
                        x =>
                        {
                            x.Embed = new BlackJackEmbedBuilder(
                                this,
                                $"**Result:** You win **{reward.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                            )
                                .WithColor(Color.Green)
                                .Build();
                            x.Components = new ComponentBuilder().Build();
                        }
                    )
                    .ConfigureAwait(false);
                OnGameEnded(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win));
                return;
            }
        }

        await Message
            .ModifyAsync(x => x.Embed = new BlackJackEmbedBuilder(this).Build())
            .ConfigureAwait(false);
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
                await Message
                    .ModifyAsync(
                        x =>
                        {
                            x.Embed = new BlackJackEmbedBuilder(
                                this,
                                $"**Result:** You win **{reward.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                            )
                                .WithColor(Color.Green)
                                .Build();
                            x.Components = new ComponentBuilder().Build();
                        }
                    )
                    .ConfigureAwait(false);
                OnGameEnded(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win));
                return;
            }
            case 21:
            {
                await Message
                    .ModifyAsync(
                        x =>
                        {
                            x.Embed = new BlackJackEmbedBuilder(
                                this,
                                $"**Result:** You lose **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                            )
                                .WithColor(Color.Red)
                                .Build();
                            x.Components = new ComponentBuilder().Build();
                        }
                    )
                    .ConfigureAwait(false);
                OnGameEnded(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose));
                return;
            }
        }

        if (PlayerScore == 21)
        {
            var reward = (int)(Bet * 2.5) - Bet;
            await Message
                .ModifyAsync(
                    x =>
                    {
                        x.Embed = new BlackJackEmbedBuilder(
                            this,
                            $"**Result:** You win **{reward.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                        )
                            .WithColor(Color.Green)
                            .Build();
                        x.Components = new ComponentBuilder().Build();
                    }
                )
                .ConfigureAwait(false);
            OnGameEnded(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win));
            return;
        }

        if (PlayerScore > DealerScore)
        {
            var reward = Bet * 2 - Bet;
            await Message
                .ModifyAsync(
                    x =>
                    {
                        x.Embed = new BlackJackEmbedBuilder(
                            this,
                            $"**Result:** You win **{reward.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                        )
                            .WithColor(Color.Green)
                            .Build();
                        x.Components = new ComponentBuilder().Build();
                    }
                )
                .ConfigureAwait(false);
            OnGameEnded(new GameEndEventArgs(UserId, Bet, reward, GameResult.Win));
            return;
        }

        if (PlayerScore < DealerScore)
        {
            await Message
                .ModifyAsync(
                    x =>
                    {
                        x.Embed = new BlackJackEmbedBuilder(
                            this,
                            $"**Result:** You lose **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                        )
                            .WithColor(Color.Red)
                            .Build();
                        x.Components = new ComponentBuilder().Build();
                    }
                )
                .ConfigureAwait(false);
            OnGameEnded(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose));
            return;
        }

        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new BlackJackEmbedBuilder(
                        this,
                        "**Result:** Tie - You get your bet back!"
                    )
                        .WithColor(Color.Blue)
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        OnGameEnded(new GameEndEventArgs(UserId, Bet, 0, GameResult.Tie));
    }

    public string GetTablePicUrl()
    {
        List<Stream> dealerImages = new();
        if (Hidden)
        {
            dealerImages.Add(DealerCards[0].GetImage());
            dealerImages.Add(
                Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("KBot.Resources.gambling.empty.png")!
            );
        }
        else
        {
            dealerImages = DealerCards.ConvertAll(card => card.GetImage());
        }

        var playerImages = PlayerCards.ConvertAll(card => card.GetImage());
        var pMerged = MergeCardImages(playerImages);
        var dMerged = MergeCardImages(dealerImages);
        var stream = MergePlayerAndDealer(pMerged, dMerged);
        var id = Guid.NewGuid().ToShortId();
        var upParams = new ImageUploadParams
        {
            File = new FileDescription($"blackjack-{id}.png", stream),
            PublicId = $"blackjack-{id}"
        };
        var result = CloudinaryClient.Upload(upParams);
        return result.Url.ToString();
    }

    private static int GetCardsValue(List<Card> cards)
    {
        var value = 0;
        var aces = 0;
        foreach (var card in cards)
        {
            if (card.Face is Face.Ace)
            {
                aces++;
                continue;
            }

            value += card.Value;
        }

        for (var i = 0; i < aces; i++)
            if (value + 11 <= 21)
                value += 11;
            else
                value++;

        return value;
    }

    private static Stream MergeCardImages(IEnumerable<Stream> images)
    {
        var bitmaps = images.ToList().ConvertAll(SKBitmap.Decode);
        var width = 0;
        var height = 0;

        foreach (var image in bitmaps)
        {
            width += image.Width;
            height = image.Height > height ? image.Height : height;
        }

        using var surface = SKSurface.Create(
            new SKImageInfo(width - bitmaps[0].Width + 21, height)
        );
        using var canvas = surface.Canvas;
        var localWidth = 0;
        foreach (var image in bitmaps)
        {
            canvas.DrawBitmap(image, localWidth, 0);
            localWidth += 15;
        }
        return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }

    private static Stream MergePlayerAndDealer(Stream player, Stream dealer)
    {
        var playerBitmap = SKBitmap.Decode(player);
        var dealerBitmap = SKBitmap.Decode(dealer);
        var height =
            playerBitmap.Height > dealerBitmap.Height ? playerBitmap.Height : dealerBitmap.Height;

        using var surface = SKSurface.Create(new SKImageInfo(360, height));
        using var canvas = surface.Canvas;
        canvas.DrawBitmap(playerBitmap, 0, 0);
        canvas.DrawBitmap(dealerBitmap, 188, 0);
        return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }

    private void OnGameEnded(GameEndEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}
