using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using CloudinaryDotNet;
using Discord;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Models;
using Discordance.Modules.Gambling;
using Discordance.Modules.Gambling.BlackJack;
using Discordance.Modules.Gambling.Crash;
using Discordance.Modules.Gambling.HighLow;
using Discordance.Modules.Gambling.Mines;
using Discordance.Modules.Gambling.Tower.Game;
using Discordance.Modules.Gambling.Towers;

namespace Discordance.Services;

public class GameService
{
    private readonly List<IGame> _games = new();
    private readonly MongoService _databaseService;
    private readonly Cloudinary _cloudinary;
    private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();

    public GameService(MongoService databaseService, Cloudinary cloudinary)
    {
        _databaseService = databaseService;
        _cloudinary = cloudinary;
    }

    public bool TryGetGame(ulong userId, out IGame? game)
    {
        game = _games.Find(x => x.UserId == userId);
        return game is not null;
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
        game = _games.Find(x => x.UserId == userId);
        if (game is not null)
            return false;

        switch (gameType)
        {
            case GameType.Blackjack:
                game = new BlackJackGame(userId, message, bet, _cloudinary);
                break;
            case GameType.Crash:
                game = new CrashGame(userId, message, bet, GenerateCrashPoint());
                break;
            case GameType.Highlow:
                game = new HighLowGame(userId, message, bet, _cloudinary);
                break;
            case GameType.Mines:
                game = new MinesGame(message, userId, bet, mines ?? 5);
                break;
            case GameType.Towers:
                game = new TowerGame(userId, message, bet, difficulty ?? Difficulty.Easy);
                break;
            default:
                return false;
        }
        game.GameEnded += OnGameEndedAsync;
        _games.Add(game);
        return true;
    }

    private async void OnGameEndedAsync(object? sender, GameEndEventArgs e)
    {
        var game = (IGame)sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.Remove(game);

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
