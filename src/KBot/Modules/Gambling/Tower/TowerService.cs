using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Gambling.Tower.Game;
using KBot.Services;

namespace KBot.Modules.Gambling.Tower;

public class TowerService : IInjectable
{
    private readonly List<TowerGame> _games = new();
    private readonly MongoService _mongo;

    public TowerService(MongoService mongo)
    {
        _mongo = mongo;
    }

    public TowerGame CreateGame(
        SocketGuildUser user,
        IUserMessage message,
        int bet,
        Difficulty difficulty
    )
    {
        var game = new TowerGame(user, message, bet, difficulty);
        _games.Add(game);
        game.GameEnded += HandleGameEndedAsync;
        return game;
    }

    private async void HandleGameEndedAsync(object? sender, GameEndedEventArgs e)
    {
        var game = (TowerGame)sender!;
        game.GameEnded -= HandleGameEndedAsync;
        _games.Remove(game);
        if (e.IsWin)
        {
            await _mongo
                .AddTransactionAsync(
                    new Transaction(e.GameId, TransactionType.Towers, e.Prize, e.Description),
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
                new Transaction(e.GameId, TransactionType.Towers, -e.Bet, e.Description),
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

    public TowerGame? GetGame(string id)
    {
        return _games.Find(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
