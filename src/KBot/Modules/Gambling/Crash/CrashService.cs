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
using KBot.Models.Guild;
using KBot.Models.User;
using KBot.Services;

namespace KBot.Modules.Gambling.Crash;

public class CrashService : IInjectable
{
    private readonly DatabaseService Database;
    private readonly List<CrashGame> Games = new();
    private readonly RandomNumberGenerator Generator = RandomNumberGenerator.Create();

    public CrashService(DatabaseService database)
    {
        Database = database;
    }

    public CrashGame CreateGame(SocketUser user, IUserMessage msg, int bet)
    {
        var crashPoint = GenerateCrashPoint();
        var game = new CrashGame(user, msg, bet, crashPoint);
        Games.Add(game);
        game.GameEnded += OnGameEndedAsync;
        return game;
    }

    private async void OnGameEndedAsync(object sender, GameEndedEventArgs e)
    {
        var game = (CrashGame) sender!;
        game.GameEnded -= OnGameEndedAsync;
        Games.Remove(game);
        await Database.UpdateUserAsync(game.Guild, game.User, x =>
        {
            if (e.IsWin)
            {
                x.Gambling.Balance += e.Prize;
                x.Gambling.Wins++;
                x.Gambling.MoneyWon += e.Prize - game.Bet;
                x.Transactions.Add(new Transaction(e.GameId, TransactionType.Gambling, e.Prize, e.Description));
            }
            else
            {
                x.Gambling.Losses++;
                x.Gambling.MoneyLost += game.Bet;
            }
        }).ConfigureAwait(false);
        if (e.IsWin)
        {
            await Database.UpdateBotUserAsync(game.Guild, x => x.Money -= e.Prize).ConfigureAwait(false);
        }
        
    }

    private double GenerateCrashPoint()
    {
        var e = Math.Pow(2, 256);
        var h = Generator.NextDouble(0, e - 1);
        return 0.80 * e / (e - h);
    }

    public CrashGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }

    public async Task StopGameAsync(string id)
    {
        var game = Games.Find(x => x.Id == id);
        if (game == null)
            return;
        await game.StopAsync().ConfigureAwait(false);
    }
}

public sealed class CrashGame : IGamblingGame
{
    public string Id { get; }
    public SocketUser User { get; }
    private IUserMessage Message { get; }
    public IGuild Guild => ((ITextChannel) Message.Channel).Guild;
    public int Bet { get; }
    private double CrashPoint { get; }
    public double Multiplier { get; private set; }
    public int Profit => (int)((Bet * Multiplier) - Bet);
    private CancellationTokenSource TokenSource { get; }
    private CancellationToken StoppingToken { get; }
    public event EventHandler<GameEndedEventArgs> GameEnded;

    public CrashGame(
        SocketUser user,
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
                OnGameEnded(new GameEndedEventArgs(Id, 0, "", false));
                await Message.ModifyAsync(x =>
                {
                    x.Embed = new EmbedBuilder().CrashEmbed(this, $"Crashed at: `{CrashPoint:0.00}x`\nYou lost **{Bet}** credits`", Color.Red);
                    x.Components = new ComponentBuilder().Build();
                }).ConfigureAwait(false);
                break;
            }
            await Task.Delay(2000).ConfigureAwait(false);
        }
    }

    public Task StopAsync()
    {
        TokenSource.Cancel();
        OnGameEnded(new GameEndedEventArgs(Id, Bet+Profit, $"CR - {Multiplier:0.0}x", false));
        return Message.ModifyAsync(x =>
        {
             x.Embed = new EmbedBuilder().CrashEmbed(this, $"Stopped at: `{Multiplier:0.00}x`\n" +
                                                           $"Crashpoint: `{CrashPoint:0.00}x`\n" +
                                                           $"You won **{Profit:0}** credits", Color.Green);
             x.Components = new ComponentBuilder().Build();
        });
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}