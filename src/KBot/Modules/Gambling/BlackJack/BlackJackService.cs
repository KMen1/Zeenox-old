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
using KBot.Models.User;
using KBot.Modules.Gambling.Objects;
using KBot.Services;
using Color = Discord.Color;
using Face = KBot.Enums.Face;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackService : IInjectable
{
    private readonly Cloudinary Cloudinary;
    private readonly DatabaseService Database;
    private readonly List<BlackJackGame> Games = new();

    public BlackJackService(DatabaseService database, Cloudinary cloudinary)
    {
        Database = database;
        Cloudinary = cloudinary;
    }

    public BlackJackGame CreateGame(SocketUser user, IUserMessage message, int stake)
    {
        var game = new BlackJackGame(user, message, stake, Cloudinary);
        Games.Add(game);
        game.GameEnded += OnGameEndedAsync;
        return game;
    }

    private async void OnGameEndedAsync(object sender, GameEndedEventArgs e)
    {
        var game = (BlackJackGame) sender!;
        game.GameEnded -= OnGameEndedAsync;
        Games.Remove(game);
        await Database.UpdateUserAsync(game.Guild, game.User, x =>
        {
            if (e.IsWin)
            {
                x.Gambling.Balance += e.Prize;
                x.Gambling.Wins++;
                x.Gambling.MoneyWon += e.Prize - game.Bet;
                x.Transactions.Add(new Transaction(e.GameId, TransactionType.Gambling, e.Prize, e.Description));
            }
            else if (e.Prize == -1)
            {
                x.Gambling.Balance += game.Bet;
            }
            else
            {
                x.Gambling.Losses++;
                x.Gambling.MoneyLost += game.Bet;
            }
        }).ConfigureAwait(false);
        if (e.IsWin || e.Prize == -1)
            await Database.UpdateBotUserAsync(game.Guild, x => x.Money -= e.Prize).ConfigureAwait(false);
    }

    public BlackJackGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }
}

public sealed class BlackJackGame : IGamblingGame
{
    public BlackJackGame(
        SocketUser player,
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
    public SocketUser User { get; }
    private IUserMessage Message { get; }
    public IGuild Guild => ((ITextChannel) Message.Channel).Guild;
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
                        $"😭 Dealer Wins! (PLAYER BUST)\nYou lost **{Bet}** credits!",
                        Color.Red);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, 0, "BL - Lose", false));
                return;
            }
            case 21:
            {
                Hidden = false;
                var reward = (int) (Bet * 2.5);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"🥳 Player Wins! (PLAYER BLACKJACK)\nYou won **{reward}** credits!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, reward, "BL - BLACKJACK", true));
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
                var reward = Bet * 2;
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"🥳 Player Wins! (DEALER BUST)\nYou won **{Bet}** credits!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, reward, "BL - DEALERBUST", true));
                return;
            }
            case 21:
            {
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"😭 Dealer Wins! (BLACKJACK)\nYou lost **{Bet}** credits!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, 0, "BL - Lose", false));
                return;
            }
        }

        if (PlayerScore == 21)
        {
            var reward = (int) (Bet * 2.5);
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"🥳 Player Wins! (BLACKJACK)\nYou won **{Bet}** credits!",
                    Color.Green);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, reward, "BL - BLACKJACK", true));
            return;
        }

        if (PlayerScore > DealerScore)
        {
            var reward = Bet * 2;
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"🥳 Player Wins!\nYou won **{Bet}** credits!",
                    Color.Green);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, reward, "BL - WIN", true));
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
            OnGameEnded(new GameEndedEventArgs(Id, 0, "BL - Win", false));
            return;
        }

        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().BlackJackEmbed(
                this,
                "😕 Tie! (PUSH)\n**The bet has been given back!**",
                Color.Blue);
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, -1, "BL - PUSH", false));
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