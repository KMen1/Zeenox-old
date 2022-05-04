using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Gambling.Mine.Game;
using KBot.Services;

namespace KBot.Modules.Gambling.Mine;

public class MineService : IInjectable
{
    private readonly List<MinesGame> _games = new();
    private readonly MongoService _mongo;

    public MineService(MongoService mongo)
    {
        _mongo = mongo;
    }

    public MinesGame CreateGame(SocketGuildUser user, IUserMessage message, int bet, int mines)
    {
        var game = new MinesGame(message, user, bet, mines);
        game.GameEnded += OnGameEndedAsync;
        _games.Add(game);
        return game;
    }

    private async void OnGameEndedAsync(object? sender, GameEndedEventArgs e)
    {
        var game = (MinesGame)sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.Remove(game);

        if (e.IsWin)
        {
            await _mongo
                .AddTransactionAsync(
                    new Transaction(e.GameId, TransactionType.Mines, e.Prize, e.Description),
                    e.User
                )
                .ConfigureAwait(false);

            await _mongo
                .UpdateUserAsync(
                    e.User,
                    x =>
                    {
                        x.Balance += e.Prize;
                        x.Wins++;
                        x.MoneyWon += e.Prize;
                    }
                )
                .ConfigureAwait(false);
            return;
        }

        await _mongo
            .AddTransactionAsync(
                new Transaction(e.GameId, TransactionType.Mines, -e.Bet, e.Description),
                e.User
            )
            .ConfigureAwait(false);

        await _mongo
            .UpdateUserAsync(
                e.User,
                x =>
                {
                    x.Balance -= e.Bet;
                    x.Losses++;
                    x.MoneyLost += e.Bet;
                }
            )
            .ConfigureAwait(false);
    }

    public MinesGame? GetGame(string id)
    {
        return _games.Find(x => x.Id.Equals(id, StringComparison.Ordinal));
    }
}
