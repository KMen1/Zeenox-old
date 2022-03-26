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
using KBot.Models;
using KBot.Modules.Gambling.Objects;
using KBot.Services;
using Color = Discord.Color;
using Face = KBot.Enums.Face;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackService
{
    private readonly DatabaseService Database;
    private readonly Cloudinary Cloudinary;
    private readonly List<BlackJackGame> Games = new();
    
    public BlackJackService(DatabaseService database, Cloudinary cloudinary)
    {
        Database = database;
        Cloudinary = cloudinary;
    }

    public BlackJackGame CreateGame(string id, SocketUser user, IUserMessage message, int stake)
    {
        var game = new BlackJackGame(id, user, message, stake, Database, Cloudinary, Games);
        Games.Add(game);
        return game;
    }
    
    public BlackJackGame GetGame(string id)
    {
        return Games.FirstOrDefault(x => x.Id == id);
    }
}

public class BlackJackGame : IGamblingGame
{
    public string Id { get; }
    private Deck Deck { get; }
    public SocketUser User { get; }
    private IUserMessage Message { get; }
    private IGuild Guild => ((ITextChannel)Message.Channel).Guild;
    private List<Card> DealerCards { get; }

    public int DealerScore => GetCardsValue(DealerCards);
    private List<Card> PlayerCards { get; }
    public int PlayerScore => GetCardsValue(PlayerCards);
    public int Stake { get; private set; }
    public bool Hidden { get; private set; }
    private Cloudinary CloudinaryClient { get; }
    private List<BlackJackGame> Container { get; }
    private DatabaseService Database { get; }

    public BlackJackGame(
        string id,
        SocketUser player,
        IUserMessage message,
        int stake,
        DatabaseService database,
        Cloudinary cloudinary,
        List<BlackJackGame> container)
    {
        Container = container;
        Id = id;
        Message = message;
        Database = database;
        Deck = new Deck();
        User = player;
        Stake = stake;
        Hidden = true;
        CloudinaryClient = cloudinary;
        DealerCards = Deck.DealHand();
        PlayerCards = Deck.DealHand();
    }

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
                await Database.UpdateUserAsync(Guild, User, x =>
                {
                    x.Gambling.Losses++;
                    x.Gambling.MoneyLost += Stake;
                }).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"😭 Az osztó nyert! (PLAYER BUST)\n**{Stake}** 🪙KCoin-t veszítettél!",
                        Color.Red);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                return;
            }
            case 21:
            {
                Hidden = false;
                Stake = (int)(Stake * 2.5);
                await Database.UpdateUserAsync(Guild, User, x =>
                {
                    x.Gambling.Balance += Stake;
                    x.Gambling.Wins++;
                    x.Gambling.MoneyWon += Stake;
                    x.Transactions.Add(new Transaction(Id, TransactionType.Gambling, Stake, "BL - BlackJack"));
                }).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"🥳 Játékos nyert! (PLAYER BLACKJACK)\n**{Stake}** 🪙KCoin-t szereztél!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                return;
            }
        }
        await Message.ModifyAsync(x => x.Embed = new EmbedBuilder().BlackJackEmbed(this)).ConfigureAwait(false);
    }

    public async Task StandAsync()
    {
        Hidden = false;
        while (DealerScore < 17)
        {
            DealerCards.Add(Deck.Draw());
        }
        switch (DealerScore)
        {
            case > 21:
            {
                Stake *= 2;
                await Database.UpdateUserAsync(Guild, User, x =>
                {
                    x.Gambling.Balance += Stake;
                    x.Gambling.Wins++;
                    x.Gambling.MoneyWon += Stake;
                    x.Transactions.Add(new Transaction(Id, TransactionType.Gambling, Stake, "BL - DEALERBUST"));
                }).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"🥳 A játékos nyert! (DEALER BUST)\n**{Stake}** 🪙KCoin-t szereztél!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                return;
            }
            case 21:
            {
                await Database.UpdateUserAsync(Guild, User, x =>
                {
                    x.Gambling.Losses++;
                    x.Gambling.MoneyLost += Stake;
                }).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().BlackJackEmbed(
                        this,
                        $"😭 Az osztó nyert! (BLACKJACK)\n**{Stake}** 🪙KCoin-t vesztettél!",
                        Color.Green);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                return;
            }
        }

        if (PlayerScore == 21)
        {
            Stake = (int)(Stake * 2.5);
            await Database.UpdateUserAsync(Guild, User, x =>
            {
                x.Gambling.Balance += Stake;
                x.Gambling.Wins++;
                x.Gambling.MoneyWon += Stake;
                x.Transactions.Add(new Transaction(Id, TransactionType.Gambling, Stake, "BL - BLACKJACK"));
            }).ConfigureAwait(false);
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"🥳 A játékos nyert! (BLACKJACK)\n**{Stake}** 🪙KCoin-t szereztél!",
                    Color.Green);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            Container.Remove(this);
            return;
        }
        if (PlayerScore > DealerScore)
        {
            Stake *= 2;
            await Database.UpdateUserAsync(Guild, User, x =>
            {
                x.Gambling.Balance += Stake;
                x.Gambling.Wins++;
                x.Gambling.MoneyWon += Stake;
                x.Transactions.Add(new Transaction(Id, TransactionType.Gambling, Stake, "BL - WIN"));
            }).ConfigureAwait(false);
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"🥳 A játékos nyert!\n**{Stake}** 🪙KCoin-t szereztél!",
                    Color.Green);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            Container.Remove(this);
            return;
        }
        if (PlayerScore < DealerScore)
        {
            await Database.UpdateUserAsync(Guild, User, x =>
            {
                x.Gambling.Losses++;
                x.Gambling.MoneyLost += Stake;
            }).ConfigureAwait(false);
            await Message.ModifyAsync(x =>
            {
                x.Embed = new EmbedBuilder().BlackJackEmbed(
                    this,
                    $"😭 Az osztó nyert!\n**{Stake}** 🪙KCoin-t vesztettél!",
                    Color.Red);
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            Container.Remove(this);
            return;
        }
        await Database.UpdateUserAsync(Guild, User, x => x.Gambling.Balance += Stake).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().BlackJackEmbed(
                this,
                "😕 Döntetlen! (PUSH)\n**A tét visszaadásra került!**",
                Color.Blue);
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }

    public string GetTablePicUrl()
    {
        List<Bitmap> dealerImages = new();
        if (Hidden)
        {
            dealerImages.Add(DealerCards[0].GetImage());
            dealerImages.Add((Bitmap) Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("KBot.Resources.empty.png")!));
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
        {
            if (value + 11 <= 21)
            {
                value += 11;
            }
            else
            {
                value++;
            }
        }

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
}