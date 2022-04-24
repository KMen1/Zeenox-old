using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Extensions;
using KBot.Models;
using KBot.Services;

namespace KBot.Modules.Gambling.Crash;

public class CrashService : IInjectable
{
    private readonly MongoService _mongo;
    private readonly List<CrashGame> _games = new();
    private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();

    public CrashService(MongoService mongo)
    {
        _mongo = mongo;
    }

    public CrashGame CreateGame(SocketGuildUser user, IUserMessage msg, int bet)
    {
        var crashPoint = GenerateCrashPoint();
        var game = new CrashGame(user, msg, bet, crashPoint);
        _games.Add(game);
        game.GameEnded += OnGameEndedAsync;
        return game;
    }

    private async void OnGameEndedAsync(object? sender, GameEndedEventArgs e)
    {
        var game = (CrashGame) sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.Remove(game);
        if (e.IsWin)
        {
            await _mongo.AddTransactionAsync(new Transaction(
                e.GameId,
                TransactionType.Crash,
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
        await _mongo.AddTransactionAsync(new Transaction(
                e.GameId,
                TransactionType.Crash,
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

    private double GenerateCrashPoint()
    {
        var e = Math.Pow(2, 256);
        var h = _generator.NextDouble(0, e - 1);
        return 0.80 * e / (e - h);
    }

    public CrashGame? GetGame(string id)
    {
        return _games.Find(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task StopGameAsync(string id)
    {
        var game = _games.Find(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (game == null)
            return;
        await game.StopAsync().ConfigureAwait(false);
    }
}

public sealed class CrashGame : IGame
{
    public CrashGame(
        SocketGuildUser user,
        IUserMessage message,
        int bet,
        double crashPoint)
    {
        Id = Guid.NewGuid().ToShortId();
        User = user;
        Message = message;
        Bet = bet;
        CrashPoint = crashPoint;
        TokenSource = new CancellationTokenSource();
        StoppingToken = TokenSource.Token;
    }

    public string Id { get; }
    public SocketGuildUser User { get; }
    private IUserMessage Message { get; }
    public int Bet { get; }
    private double CrashPoint { get; }
    public double Multiplier { get; private set; }
    public int Profit => (int) (Bet * Multiplier - Bet);
    private CancellationTokenSource TokenSource { get; }
    private CancellationToken StoppingToken { get; }
    public event EventHandler<GameEndedEventArgs>? GameEnded;

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
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().CrashEmbed(this,
                        $"**Crashed at:** {CrashPoint:0.00}x\n**Result:** You lost **{Bet}** credits", Color.Red);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "Crash: LOSE", false));
                break;
            }

            await Task.Delay(2000).ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        TokenSource.Cancel();
        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().CrashEmbed(this, $"**Stopped at:** {Multiplier:0.00}x\n" +
                                                          $"**Crashpoint:** {CrashPoint:0.00}x\n" +
                                                          $"**Result:** You win **{Profit:0}** credits", Color.Green);
            x.Components = new ComponentBuilder().Build();
        }).ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, Profit, $"Crash: {Multiplier:0.0}x", false));
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}