using System;
using System.Collections.Generic;
using CloudinaryDotNet;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Gambling.HighLow.Game;
using KBot.Services;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowService : IInjectable
{
    private readonly Cloudinary _cloudinary;
    private readonly List<HighLowGame> _games = new();
    private readonly MongoService _mongo;

    public HighLowService(MongoService database, Cloudinary cloudinary)
    {
        _mongo = database;
        _cloudinary = cloudinary;
    }

    public HighLowGame CreateGame(SocketGuildUser user, IUserMessage message, int stake)
    {
        var game = new HighLowGame(user, message, stake, _cloudinary);
        game.GameEnded += OnGameEndedAsync;
        _games.Add(game);
        return game;
    }

    private async void OnGameEndedAsync(object? sender, GameEndedEventArgs e)
    {
        var game = (HighLowGame) sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.Remove(game);

        if (e.IsWin)
        {
            await _mongo.AddTransactionAsync(new Transaction(
                    e.GameId,
                    TransactionType.Highlow,
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
                TransactionType.Highlow,
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

    public HighLowGame? GetGame(string id)
    {
        return _games.Find(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}