using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Gambling.Objects;
using KBot.Services;

namespace KBot.Modules.Gambling.Crash;

public class CrashService
{
    private readonly DatabaseService Database;
    private readonly List<CrashGame> Games = new();
    private readonly RandomNumberGenerator Generator = RandomNumberGenerator.Create();

    public CrashService(DatabaseService database)
    {
        Database = database;
    }

    public CrashGame CreateGame(SocketUser user, IUserMessage msg, int bet)
    {
        var crashPoint = GenerateCrashPoint();
        var game = new CrashGame(user, msg, bet, crashPoint, Games, Database);
        Games.Add(game);
        return game;
    }

    private double GenerateCrashPoint()
    {
        var e = Math.Pow(2, 256);
        var h = Generator.NextDouble(0, e - 1);
        return 0.80 * e / (e - h);
    }

    public CrashGame GetGame(string id)
    {
        return Games.FirstOrDefault(x => x.Id == id);
    }

    public async Task StopGameAsync(string id)
    {
        var game = Games.Find(x => x.Id == id);
        if (game == null)
            return;
        await game.StopAsync().ConfigureAwait(false);
    }
}

public class CrashGame : IGamblingGame
{
    public string Id { get; }
    public SocketUser User { get; }
    private IUserMessage Message { get; }
    private IGuild Guild => ((ITextChannel) Message.Channel).Guild;
    public int Bet { get; }
    private double CrashPoint { get; }
    public double Multiplier { get; private set; }
    public int Profit => (int)((Bet * Multiplier) - Bet);
    private DatabaseService Db { get; }
    private List<CrashGame> Container { get; }
    private CancellationTokenSource TokenSource { get; }
    private CancellationToken StoppingToken { get; }

    public CrashGame(
        SocketUser user,
        IUserMessage message,
        int bet,
        double crashPoint,
        List<CrashGame> container,
        DatabaseService db)
    {
        Id = Guid.NewGuid().ConvertToGameId();
        User = user;
        Message = message;
        Bet = bet;
        CrashPoint = crashPoint;
        Container = container;
        TokenSource = new CancellationTokenSource();
        StoppingToken = TokenSource.Token;
        Db = db;
    }

    public async Task StartAsync()
    {
        await Message.ModifyAsync(x =>
        {
            x.Content = string.Empty;
            x.Embed = new EmbedBuilder().CrashEmbed(this);
            x.Components = new ComponentBuilder()
                .WithButton(" ", $"crash:{Id}", ButtonStyle.Danger, new Emoji("🛑"))
                .Build();
        }).ConfigureAwait(false);
        Multiplier = 1.00;
        while (!StoppingToken.IsCancellationRequested)
        {
            Multiplier += 0.10;
            await Message.ModifyAsync(x => x.Embed = new EmbedBuilder().CrashEmbed(this)).ConfigureAwait(false);

            if (Multiplier >= CrashPoint)
            {
                await Db.UpdateUserAsync(Guild, User, x =>
                {
                    x.Gambling.Losses++;
                    x.Gambling.MoneyLost += Bet;
                }).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().CrashEmbed(this, $"Crashelt itt: `{CrashPoint:0.00}x`\nVesztettél: `{Bet} kreditet`", Color.Red);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                Container.Remove(this);
                break;
            }
            await Task.Delay(2000).ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        TokenSource.Cancel();
        _ = Task.Run(async () => await Db.UpdateUserAsync(Guild, User, x =>
        {
            x.Gambling.Balance += (int)Math.Round(Bet * Multiplier);
            x.Gambling.Wins++;
            x.Gambling.MoneyWon += (int)(Bet * Multiplier - Bet);
            x.Transactions.Add(new Transaction(Id, TransactionType.Gambling, (int)Math.Round(Bet * Multiplier), $"CR - {Multiplier:0.0}x"));
        }).ConfigureAwait(false));
        _ = Task.Run(async () => await Db.UpdateBotUserAsync(Guild,x => x.Money -= (int)Math.Round(Bet * Multiplier)).ConfigureAwait(false));
        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().CrashEmbed(this, $"Kivetted itt: `{Multiplier:0.00}x`\nCrashelt volna: `{CrashPoint:0.00}x`\nNyertél: `{Profit:0} kreditet`", Color.Green);
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }
}