using System;
using System.Collections.Generic;
using CloudinaryDotNet;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Gambling.BlackJack.Game;
using KBot.Services;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackService : IInjectable
{
    private readonly Cloudinary _cloudinary;
    private readonly List<BlackJackGame> _games = new();
    private readonly MongoService _mongo;

    public BlackJackService(MongoService mongo, Cloudinary cloudinary)
    {
        _mongo = mongo;
        _cloudinary = cloudinary;
    }

    public BlackJackGame CreateGame(SocketGuildUser user, IUserMessage message, int stake)
    {
        var game = new BlackJackGame(user, message, stake, _cloudinary);
        _games.Add(game);
        game.GameEnded += OnGameEndedAsync;
        return game;
    }

    private async void OnGameEndedAsync(object? sender, GameEndedEventArgs e)
    {
        var game = (BlackJackGame)sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.Remove(game);

        if (e.IsWin)
        {
            await _mongo
                .AddTransactionAsync(
                    new Transaction(e.GameId, TransactionType.Blackjack, e.Prize, e.Description),
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

        if (e.Prize == -1)
            return;
        await _mongo
            .AddTransactionAsync(
                new Transaction(e.GameId, TransactionType.Blackjack, -e.Bet, e.Description),
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

    public BlackJackGame? GetGame(string id)
    {
        return _games.Find(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
