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
using KBot.Extensions;
using KBot.Models;
using KBot.Modules.Gambling.Objects;
using KBot.Services;
using Color = Discord.Color;
using Face = KBot.Enums.Face;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackService : IInjectable
{
    private readonly Cloudinary _cloudinary;
    private readonly MongoService _mongo;
    private readonly List<BlackJackGame> _games = new();

    public BlackJackService(MongoService mongo, Cloudinary cloudinary)
    {
        _mongo = mongo;
        _cloudinary = cloudinary;
    }

    public BlackJackGame CreateGame(SocketGuildUser user, IUserMessage message, int stake)
    {
        var game = new BlackJackGame(user, message, stake, _cloudinary);
        _games.Add(game);
        game.GameEnded += OnGameEndedAsync;
        return game;
    }

    private async void OnGameEndedAsync(object sender, GameEndedEventArgs e)
    {
        var game = (BlackJackGame) sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.Remove(game);

        if (e.IsWin)
        {
            await _mongo.AddTransactionAsync(new Transaction(
                e.GameId,
                TransactionType.Blackjack,
                e.Prize,
                e.Description),
                e.User).ConfigureAwait(false);

            await _mongo.UpdateUserAsync(e.User, x =>
            {
                x.Balance += e.Prize;
                x.Wins++;
                x.MoneyWon += e.Prize;
            }).ConfigureAwait(false);
            return;
        }
        if (e.Prize == -1)
        {
            return;
        }
        await _mongo.AddTransactionAsync(new Transaction(
            e.GameId,
            TransactionType.Blackjack,
            -e.Bet,
            e.Description),
            e.User).ConfigureAwait(false);
            
        await _mongo.UpdateUserAsync(e.User, x =>
        {
            x.Balance -= e.Bet;
            x.Losses++;
            x.MoneyLost += e.Bet;
        }).ConfigureAwait(false);
    }

    public BlackJackGame GetGame(string id)
    {
        return _games.Find(x => x.Id == id);
    }
}

public sealed class BlackJackGame : IGamblingGame
{
    public BlackJackGame(
        SocketGuildUser player,
        IUserMessage message,
        int bet,
        Cloudinary cloudinary)
    {
        Id = Guid.NewGuid().ToShortId();
        Message = message;
        Deck = new Deck();
        User = player;
        Bet = bet;
        Hidden = true;
        CloudinaryClient = cloudinary;
        DealerCards = Deck.DealHand();
        PlayerCards = Deck.DealHand();
    }

    public string Id { get; }
    private Deck Deck { get; }
    public SocketGuildUser User { get; }
    private IUserMessage Message { get; }
    private List<Card> DealerCards { get; }

    public int DealerScore => GetCardsValue(DealerCards);
    private List<Card> PlayerCards { get; }
    public int PlayerScore => GetCardsValue(PlayerCards);
    public int Bet { get; }
    public bool Hidden { get; private set; }
    private Cloudinary CloudinaryClient { get; }
    public event EventHandler<GameEndedEventArgs> GameEnded;

    public Task StartAsync()
    {
        return Message.ModifyAsync(x =>
        {
            x.Content = string.Empty;
            x.Embed = new EmbedBuilder().BlackJackEmbed(this);
            x.Components = new ComponentBuilder()
                .WithButton("Hit", $"blackjack-hit:{Id}")
                .WithButton("Stand", $"blackjack-stand:{Id}")
                .Build();
        });
    }

    public async Task HitAsync()
    {
        PlayerCards.Add(Deck.Draw());
        switch (PlayerScore)
        {
            case > 21:
            {
                Hidden = false;
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"😭 Dealer Wins!\nYou lost **{Bet}** credits!",
                        Color.Red);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "Blackjack: PLAYER BUST", false));
                return;
            }
            case 21:
            {
                Hidden = false;
                var reward = (int) (Bet * 2.5) - Bet;
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"🥳 Player Wins!\nYou won **{reward}** credits!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, User, Bet, reward, "Blackjack: PLAYER BLACKJACK", true));
                return;
            }
        }

        await Message.ModifyAsync(x => x.Embed = new EmbedBuilder().BlackJackEmbed(this)).ConfigureAwait(false);
    }

    public async Task StandAsync()
    {
        Hidden = false;
        while (DealerScore < 17) DealerCards.Add(Deck.Draw());
        switch (DealerScore)
        {
            case > 21:
            {
                var reward = (Bet * 2) - Bet;
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"🥳 Player Wins!\nYou won **{Bet}** credits!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, User, Bet, reward, "Blackjack: DEALER BUST", true));
                return;
            }
            case 21:
            {
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"😭 Dealer Wins!\nYou lost **{Bet}** credits!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "Blackjack: DEALER WIN", false));
                return;
            }
        }

        if (PlayerScore == 21)
        {
            var reward = (int) (Bet * 2.5) - Bet;
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"🥳 Player Wins!\nYou won **{reward}** credits!",
                    Color.Green);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, User, Bet, reward, "Blackjack: PLAYER BLACKJACK", true));
            return;
        }

        if (PlayerScore > DealerScore)
        {
            var reward = (Bet * 2) - Bet;
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"🥳 Player Wins!\nYou won **{reward}** credits!",
                    Color.Green);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, User, Bet, reward, "Blackjack: PLAYER WIN", true));
            return;
        }

        if (PlayerScore < DealerScore)
        {
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"😭 Dealer Wins!\nYou lost **{Bet}** credits!",
                    Color.Red);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "Blackjack: DEALER WIN", false));
            return;
        }

        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().BlackJackEmbed(
                this,
                "😕 Tie!",
                Color.Blue);
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, -1, "Blackjack: PUSH", false));
    }

    public string GetTablePicUrl()
    {
        List<Bitmap> dealerImages = new();
        if (Hidden)
        {
            dealerImages.Add(DealerCards[0].GetImage());
            dealerImages.Add((Bitmap) Image.FromStream(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("KBot.Resources.empty.png")!));
        }
        else
        {
            dealerImages = DealerCards.ConvertAll(card => card.GetImage());
        }

        var playerImages = PlayerCards.ConvertAll(card => card.GetImage());
        var pMerged = MergeImages(playerImages);
        var dMerged = MergeImages(dealerImages);
        var merged = MergePlayerAndDealer(pMerged, dMerged);
        var stream = new MemoryStream();
        merged.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        var upParams = new ImageUploadParams
        {
            File = new FileDescription($"blackjack-{Id}.png", stream),
            PublicId = $"blackjack-{Id}"
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

    private static Bitmap MergeImages(IEnumerable<Bitmap> images)
    {
        var enumerable = images as IList<Bitmap> ?? images.ToList();

        var width = 0;
        var height = 0;

        foreach (var image in enumerable)
        {
            width += image.Width;
            height = image.Height > height
                ? image.Height
                : height;
        }

        var bitmap = new Bitmap(width - enumerable[0].Width + 21, height);
        using var g = Graphics.FromImage(bitmap);
        var localWidth = 0;
        foreach (var image in enumerable)
        {
            g.DrawImage(image, localWidth, 0);
            localWidth += 15;
        }

        return bitmap;
    }

    private static Bitmap MergePlayerAndDealer(Image player, Image dealer)
    {
        var height = player.Height > dealer.Height
            ? player.Height
            : dealer.Height;

        var bitmap = new Bitmap(360, height);
        using var g = Graphics.FromImage(bitmap);
        g.DrawImage(player, 0, 0);
        g.DrawImage(dealer, 188, 0);
        return bitmap;
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}