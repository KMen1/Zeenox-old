using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Models;
using KBot.Services;
using Serilog;

namespace KBot.Modules.Gambling.Crash;

public class CrashService
{
    private readonly List<CrashGame> Games = new();
    private readonly Random Random = new();
    
    public Task StartGameAsync(string id, SocketUser user, IUserMessage message, int bet, User dbUser, DatabaseService db)
    {
        var game = new CrashGame(id, user, message, Random, bet, Games, dbUser, db);
        Games.Add(game);
        return game.Start();
    }
    public async Task StopGameAsync(string id)
    {
        var game = Games.Find(x => x.Id == id);
        if (game == null)
            return;
        await game.StopAsync().ConfigureAwait(false);
    }
}

public class CrashGame
{
    public CrashGame(string id, SocketUser user, IUserMessage message, Random random, int bet, List<CrashGame> container, User dbUser, DatabaseService db)
    {
        Id = id;
        User = user;
        Message = message;
        Bet = bet;
        CrashPoint = 999999999 / Convert.ToDecimal(random.Next(1, 1000000000));
        Container = container;
        DbUser = dbUser;
        Db = db;
    }
    public string Id { get; }
    private SocketUser User { get; }
    private CancellationTokenSource CancellationTokenSource { get; } = new();
    private User DbUser { get; }
    private DatabaseService Db { get; }
    private IUserMessage Message { get; }
    private int Bet { get; }
    private decimal CrashPoint { get; }
    private decimal Multiplier { get; set; }
    private List<CrashGame> Container { get; }

    public async Task Start()
    {
        Log.Logger.Information(CrashPoint.ToString());
            for (Multiplier = 1.0M; Multiplier < 1000000000; Multiplier += 0.2M)
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
                        .AddField("Profit", $"`{Bet*Multiplier:0}`", true)
                        .Build();
                    x.Embed = embed;
                }).ConfigureAwait(false);

                if (Multiplier >= CrashPoint)
                {
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
                    DbUser.GamblingProfile.Crash.Losses++;
                    DbUser.GamblingProfile.Crash.MoneyLost += Bet;
                    await Db.UpdateUserAsync(((SocketTextChannel)Message.Channel).Guild.Id, DbUser).ConfigureAwait(false);
                    Container.Remove(this);
                    break;
                }
                await Task.Delay(2000).ConfigureAwait(false);
            }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource.Cancel();
        DbUser.GamblingProfile.Money += (int)(Bet*Multiplier);
        DbUser.GamblingProfile.Crash.Wins++;
        DbUser.GamblingProfile.Crash.MoneyWon += (int)(Bet*Multiplier);
        await Db.UpdateUserAsync(((SocketTextChannel)Message.Channel).Guild.Id, DbUser).ConfigureAwait(false);
        await Message.ModifyAsync(x =>
        {
            var embed = new EmbedBuilder()
                .WithTitle("Crash")
                .WithColor(Color.Green)
                .AddField("Kivéve", $"`{Multiplier:0.0}x`", true)
                .AddField("Profit", $"`{Bet*Multiplier:0}`", true)
                .Build();
            x.Embed = embed;
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        Container.Remove(this);
    }
}