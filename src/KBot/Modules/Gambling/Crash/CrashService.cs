using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Extensions;
using KBot.Models;
using KBot.Modules.Gambling.Crash.Game;
using KBot.Services;

namespace KBot.Modules.Gambling.Crash;

public class CrashService : IInjectable
{
    private readonly List<CrashGame> _games = new();
    private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
    private readonly MongoService _mongo;

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