using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Modules.Gambling.Objects;
using KBot.Services;

namespace KBot.Modules.Gambling.Crash;

public class CrashService
{
    private readonly DatabaseService Database;
    private readonly List<CrashGame> Games = new();

    public CrashService(DatabaseService database)
    {
        Database = database;
    }

    public Task StartGameAsync(string id, SocketUser user, IUserMessage message, int bet)
    {
        var crashPoint = 999999999 / Convert.ToDecimal(Generators.RandomNumberBetween(1, 1000000000));
        var game = new CrashGame(id, user, message, bet, crashPoint, Games, Database);
        Games.Add(game);
        return game.StartAsync();
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
    private SocketUser User { get; }
    private IUserMessage Message { get; }
    private IGuild Guild => ((ITextChannel) Message.Channel).Guild;
    private int Bet { get; }
    private decimal CrashPoint { get; }
    private decimal Multiplier { get; set; }
    private DatabaseService Db { get; }
    private List<CrashGame> Container { get; }
    private CancellationTokenSource CancellationTokenSource { get; } = new();
    
    public CrashGame(string id, SocketUser user, IUserMessage message, int bet, decimal crashPoint,  List<CrashGame> container, DatabaseService db)
    {
        Id = id;
        User = user;
        Message = message;
        Bet = bet;
        CrashPoint = crashPoint;
        Container = container;
        Db = db;
    }

    public async Task StartAsync()
    {
        for (Multiplier = 1.0M; Multiplier < 1000000000; Multiplier += 0.1M)
        {
            if (CancellationTokenSource.Token.IsCancellationRequested)
            {
                CancellationTokenSource.Dispose();
                return;
            }
            await Message.ModifyAsync(x =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Crash")
                    .WithColor(Color.Gold)
                    .AddField("Szorzó", $"`{Multiplier:0.0}x`", true)
                    .AddField("Profit", $"`{Bet * Multiplier-Bet:0}`", true)
                    .Build();
                x.Embed = embed;
            }).ConfigureAwait(false);

            if (Multiplier >= CrashPoint)
            {
                var dbUser = await Db.GetUserAsync(Guild, User).ConfigureAwait(false);
                dbUser.GamblingProfile.Crash.Losses++;
                dbUser.GamblingProfile.Crash.MoneyLost += Bet;
                await Db.UpdateUserAsync(((SocketTextChannel)Message.Channel).Guild.Id, dbUser).ConfigureAwait(false);
                await Message.ModifyAsync(x =>
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Crash")
                        .WithColor(Color.Red)
                        .AddField("Crashelt", $"`{Multiplier:0.0}x`", true)
                        .AddField("Profit", $"`-{Bet:0}`", true)
                        .Build();
                    x.Embed = embed;
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
        CancellationTokenSource.Cancel();
        var dbUser = await Db.GetUserAsync(Guild, User).ConfigureAwait(false);
        dbUser.GamblingProfile.Money += (int)(Bet*Multiplier);
        dbUser.GamblingProfile.Crash.Wins++;
        dbUser.GamblingProfile.Crash.MoneyWon += (int)((Bet * Multiplier) - Bet);
        await Db.UpdateUserAsync(((SocketTextChannel)Message.Channel).Guild.Id, dbUser).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            var embed = new EmbedBuilder()
                .WithTitle("Crash")
                .WithColor(Color.Green)
                .AddField("Kivéve", $"`{Multiplier:0.0}x`", true)
                .AddField("Profit", $"`{(Bet * Multiplier) - Bet:0}`", true)
                .Build();
            x.Embed = embed;
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }
}