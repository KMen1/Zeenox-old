using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Discord;
using Discord.WebSocket;
using KBot.Modules.Gambling.Objects;
using KBot.Services;
using Color = Discord.Color;
using Face = KBot.Modules.Gambling.Objects.Face;
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
    public void RemoveGame(string id)
    {
        Games.Remove(GetGame(id));
    }
    public void RemoveGame(BlackJackGame game)
    {
        Games.Remove(game);
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

    private int DealerScore => GetCardsValue(DealerCards);
    private List<Card> PlayerCards { get; }
    private int PlayerScore => GetCardsValue(PlayerCards);
    private int Stake { get; set; }
    private Cloudinary CloudinaryClient { get; }
    private List<BlackJackGame> Container { get; }
    private DatabaseService Database { get; }

    public BlackJackGame(string id, SocketUser player, IUserMessage message, int stake, DatabaseService database, Cloudinary cloudinary, List<BlackJackGame> container)
    {
        Container = container;
        Id = id;
        Message = message;
        Database = database;
        Deck = new Deck();
        User = player;
        Stake = stake;
        CloudinaryClient = cloudinary;
        DealerCards = Deck.DealHand();
        PlayerCards = Deck.DealHand();
    }

    public Task StartAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle("Blackjack")
            .WithDescription($"Tét: `{Stake}`")
            .WithColor(Color.Gold)
            .WithImageUrl(GetTablePicUrl(true))
            .WithDescription($"Tét: {Stake} kredit")
            .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
            .AddField("Osztó", "Érték: `?`", true)
            .Build();
        var comp = new ComponentBuilder()
            .WithButton("Hit", $"blackjack-hit:{Id}")
            .WithButton("Stand", $"blackjack-stand:{Id}")
            .Build();
        return Message.ModifyAsync(x =>
         {
             x.Content = string.Empty;
             x.Embed = eb;
             x.Components = comp;
         });
    }

    public async Task HitAsync()
    {
        var dbUser = await Database.GetUserAsync(Guild, User).ConfigureAwait(false);
        PlayerCards.Add(Deck.Draw());
        switch (PlayerScore)
        {
            case > 21:
            {
                var eb = new EmbedBuilder()
                    .WithTitle("Blackjack")
                    .WithColor(Color.Red)
                    .WithDescription($"😭 Az osztó nyert! (PLAYER BUST)\n**{Stake}** 🪙KCoin-t veszítettél!")
                    .WithImageUrl(GetTablePicUrl(false))
                    .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
                    .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
                    .Build();
                dbUser.GamblingProfile.BlackJack.Losses++;
                await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = eb;
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                return;
            }
            case 21:
            {
                Stake = (int)(Stake * 2.5);
                var eb = new EmbedBuilder()
                    .WithTitle("Blackjack")
                    .WithColor(Color.Green)
                    .WithDescription($"🥳 Játékos nyert! (PLAYER BLACKJACK)\n**{Stake}** 🪙KCoin-t szereztél!")
                    .WithImageUrl(GetTablePicUrl(false))
                    .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
                    .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
                    .Build();
                dbUser.GamblingProfile.Money += Stake;
                dbUser.GamblingProfile.BlackJack.Wins++;
                await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = eb;
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                return;
            }
        }
        var playEb = new EmbedBuilder()
            .WithTitle("Blackjack")
            .WithColor(Color.Gold)
            .WithDescription($"Tét: `{Stake}`")
            .WithImageUrl(GetTablePicUrl(true))
            .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
            .AddField("Osztó", "Érték: `?`", true)
            .Build();
        await Message.ModifyAsync(x => x.Embed = playEb).ConfigureAwait(false);
    }

    public async Task StandAsync()
    {
        var dbUser = await Database.GetUserAsync(Guild, User).ConfigureAwait(false);
        while (DealerScore < 17)
        {
            DealerCards.Add(Deck.Draw());
        }
        switch (DealerScore)
        {
            case > 21:
            {
                var eb = new EmbedBuilder()
                    .WithTitle("Blackjack")
                    .WithColor(Color.Green)
                    .WithDescription($"🥳 A játékos nyert! (DEALER BUST)\n**{Stake}** 🪙KCoin-t szereztél!")
                    .WithImageUrl(GetTablePicUrl(false))
                    .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
                    .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
                    .Build();
                dbUser.GamblingProfile.Money += Stake;
                dbUser.GamblingProfile.BlackJack.Wins++;
                await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = eb;
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Stake *= 2;
                Container.Remove(this);
                return;
            }
            case 21:
            {
                var eb = new EmbedBuilder()
                    .WithTitle("Blackjack")
                    .WithColor(Color.Red)
                    .WithDescription($"😭 Az osztó nyert! (BLACKJACK)\n**{Stake}** 🪙KCoin-t vesztettél!")
                    .WithImageUrl(GetTablePicUrl(false))
                    .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
                    .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
                    .Build();
                dbUser.GamblingProfile.BlackJack.Losses++;
                await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = eb;
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                return;
            }
        }

        if (PlayerScore == 21)
        {
            Stake = (int)(Stake * 2.5);
            
            var eb = new EmbedBuilder()
                .WithTitle("Blackjack")
                .WithColor(Color.Green)
                .WithDescription($"🥳 A játékos nyert! (BLACKJACK)\n**{Stake}** 🪙KCoin-t szereztél!")
                .WithImageUrl(GetTablePicUrl(false))
                .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
                .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
                .Build();
            dbUser.GamblingProfile.Money += Stake;
            dbUser.GamblingProfile.BlackJack.Wins++;
            await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
            await Message.ModifyAsync(x =>
            {
                x.Embed = eb;
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            Container.Remove(this);
            return;
        }
        if (PlayerScore > DealerScore)
        {
            Stake *= 2;
            var eb = new EmbedBuilder()
                .WithTitle("Blackjack")
                .WithColor(Color.Green)
                .WithDescription($"🥳 A játékos nyert!\n**{Stake}** 🪙KCoin-t szereztél!")
                .WithImageUrl(GetTablePicUrl(false))
                .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
                .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
                .Build();
            dbUser.GamblingProfile.Money += Stake;
            dbUser.GamblingProfile.BlackJack.Wins++;
            await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
            await Message.ModifyAsync(x =>
            {
                x.Embed = eb;
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            Container.Remove(this);
            return;
        }
        if (PlayerScore < DealerScore)
        {
            var eb = new EmbedBuilder()
                .WithTitle("Blackjack")
                .WithColor(Color.Red)
                .WithDescription($"😭 Az osztó nyert!\n**{Stake}** 🪙KCoin-t vesztettél!")
                .WithImageUrl(GetTablePicUrl(false))
                .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
                .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
                .Build();
            dbUser.GamblingProfile.BlackJack.Losses++;
            await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
            await Message.ModifyAsync(x =>
            {
                x.Embed = eb;
                x.Components = new ComponentBuilder().Build();
            }).ConfigureAwait(false);
            Container.Remove(this);
            return;
        }
        var pEb = new EmbedBuilder()
            .WithTitle("Blackjack")
            .WithColor(Color.Green)
            .WithDescription("😕 Döntetlen! (PUSH)\n**A tét visszaadásra került!**")
            .WithImageUrl(GetTablePicUrl(false))
            .AddField("Játékos", $"Érték: `{PlayerScore.ToString()}`", true)
            .AddField("Osztó", $"Érték: `{DealerScore.ToString()}`", true)
            .Build();
        dbUser.GamblingProfile.Money += Stake;
        await Database.UpdateUserAsync(Guild.Id, dbUser).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            x.Embed = pEb;
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }

    private string GetTablePicUrl(bool hidden)
    {
        List<Bitmap> dealerImages = new();
        if (hidden)
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