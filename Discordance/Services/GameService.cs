using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using CloudinaryDotNet;
using Discord;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Models;
using Discordance.Modules.Gambling.Games;
using Discordance.Modules.Gambling.Tower.Game;

namespace Discordance.Services;

public class GameService
{
    private readonly Cloudinary _cloudinary;
    private readonly MongoService _databaseService;
    private readonly ConcurrentDictionary<ulong, IGame> _games;
    private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();

    public GameService(MongoService databaseService, Cloudinary cloudinary)
    {
        _databaseService = databaseService;
        _cloudinary = cloudinary;
        _games = new ConcurrentDictionary<ulong, IGame>();
    }

    public bool TryGetGame(ulong userId, out IGame? game)
    {
        _games.TryGetValue(userId, out game);
        return game is not null;
    }

    public IGame GetGame(ulong userId)
    {
        return _games[userId];
    }

    public bool IsPlaying(ulong userId)
    {
        return _games.ContainsKey(userId);
    }

    public bool TryStartGame(
        ulong userId,
        IUserMessage message,
        GameType gameType,
        int bet,
        out IGame? game,
        int? mines = 5,
        Difficulty? difficulty = Difficulty.Easy
    )
    {
        game = null;
        if (IsPlaying(userId))
            return false;

        switch (gameType)
        {
            case GameType.Blackjack:
                game = new BlackJack(userId, message, bet, _cloudinary);
                break;
            case GameType.Crash:
                game = new Crash(userId, message, bet, GenerateCrashPoint());
                break;
            case GameType.Highlow:
                game = new HighLow(userId, message, bet, _cloudinary);
                break;
            case GameType.Mines:
                game = new Mines(message, userId, bet, mines ?? 5);
                break;
            case GameType.Towers:
                game = new Towers(userId, message, bet, difficulty ?? Difficulty.Easy);
                break;
            default:
                return false;
        }

        game.GameEnded += OnGameEndedAsync;
        _games.TryAdd(userId, game);
        return true;
    }

    private async void OnGameEndedAsync(object? sender, GameEndEventArgs e)
    {
        var game = (IGame) sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.TryRemove(game.UserId, out _);

        switch (e.Result)
        {
            case GameResult.Win:
            {
                await _databaseService
                    .UpdateUserAsync(
                        e.UserId,
                        x =>
                        {
                            x.Balance += e.Prize;
                            x.Wins++;
                            x.MoneyWon += e.Prize;
                        }
                    )
                    .ConfigureAwait(false);
                break;
            }
            case GameResult.Lose:
            {
                await _databaseService
                    .UpdateUserAsync(
                        e.UserId,
                        x =>
                        {
                            x.Balance -= e.Bet;
                            x.Losses++;
                            x.MoneyLost += e.Bet;
                        }
                    )
                    .ConfigureAwait(false);
                break;
            }
            case GameResult.Tie:
                break;
        }
    }

    private double GenerateCrashPoint()
    {
        var e = Math.Pow(2, 256);
        var h = _generator.NextDouble(0, e - 1);
        return 0.80 * e / (e - h);
    }
}