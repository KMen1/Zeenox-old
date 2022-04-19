using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Extensions;
using KBot.Models;
using KBot.Services;

namespace KBot.Modules.Gambling.Mines;

public class MinesService : IInjectable
{
    private readonly MongoService _mongo;
    private readonly List<MinesGame> _games = new();

    public MinesService(MongoService mongo)
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

    private async void OnGameEndedAsync(object sender, GameEndedEventArgs e)
    {
        var game = (MinesGame) sender!;
        game.GameEnded -= OnGameEndedAsync;
        _games.Remove(game);

        if (e.IsWin)
        {
            await _mongo.AddTransactionAsync(new Transaction(
                e.GameId,
                TransactionType.Mines,
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
            TransactionType.Mines,
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

    public MinesGame GetGame(string id)
    {
        return _games.Find(x => x.Id == id);
    }
}

public sealed class MinesGame : IGamblingGame
{
    private readonly List<Point> Points = new();

    public MinesGame(
        IUserMessage message,
        SocketGuildUser user,
        int bet,
        int mines)
    {
        Id = Guid.NewGuid().ToShortId();
        Message = message;
        User = user;
        Bet = bet;
        var random = new Random();
        for (var i = 0; i < 5; i++)
        for (var j = 0; j < 5; j++)
            Points.Add(new Point {X = i, Y = j, Emoji = new Emoji("🪙"), IsClicked = false, Label = " "});

        for (var i = 0; i < mines; i++)
        {
            var index = random.Next(0, Points.Count);
            while (Points[index].IsMine) index = random.Next(0, Points.Count);
            Points[index].IsMine = true;
            Points[index].Emoji = new Emoji("💣");
        }
    }

    public string Id { get; }
    private IUserMessage Message { get; }
    public SocketGuildUser User { get; }
    public int Bet { get; }
    public bool CanStop { get; private set; }
    private int Mines => Points.Count(x => x.IsMine);
    private int Clicked => Points.Count(x => x.IsClicked && !x.IsMine);

    private decimal Multiplier
    {
        get
        {
            var szam = Factorial(25 - Mines) * Factorial(25 - Clicked);
            var oszt = Factorial(25) * Factorial(25 - Mines - Clicked);
            var t = (decimal) Math.Round(szam / oszt, 2);
            return Math.Round((decimal) .97 * (1 / t), 2);
        }
    }

    public event EventHandler<GameEndedEventArgs> GameEnded;

    public Task StartAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle($"Mines | {Id}")
            .WithColor(Color.Gold)
            .WithDescription($"Bet: **{Bet}**\nMines: **{Mines}**\nExit: `/mine stop {Id}`")
            .Build();
        var comp = new ComponentBuilder();
        var size = Math.Sqrt(Points.Count);
        for (var x = 0; x < size; x++)
        {
            var row = new ActionRowBuilder();
            for (var y = 0; y < size; y++)
                row.AddComponent(new ButtonBuilder(" ", $"mine:{Id}:{x}:{y}", emote: new Emoji("🪙")).Build());

            comp.AddRow(row);
        }

        return Message.ModifyAsync(x =>
        {
            x.Content = string.Empty;
            x.Embed = eb;
            x.Components = comp.Build();
        });
    }

    public async Task ClickFieldAsync(int x, int y)
    {
        CanStop = true;
        var point = Points.Find(point => point.X == x && point.Y == y);
        if (point!.IsMine)
        {
            await StopAsync(true).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "MN - LOSE", false));
            return;
        }

        point!.IsClicked = true;
        point.Label = $"{Multiplier}x";
        var comp = new ComponentBuilder();
        for (var i = 0; i < Math.Sqrt(Points.Count); i++)
        {
            var row = new ActionRowBuilder();
            for (var j = 0; j < Math.Sqrt(Points.Count); j++)
            {
                var tPonint = Points.Find(z => z.X == i && z.Y == j);
                row.AddComponent(new ButtonBuilder(tPonint!.Label, $"mine:{Id}:{i}:{j}", emote: new Emoji("🪙"),
                    isDisabled: tPonint.IsClicked).Build());
            }

            comp.AddRow(row);
        }

        await Message.ModifyAsync(z => z.Components = comp.Build()).ConfigureAwait(false);
    }

    private static double Factorial(int n)
    {
        if (n == 0)
            return 1;
        return n * Factorial(n - 1);
    }

    public async Task StopAsync(bool lost)
    {
        var prize = lost ? 0 : (int) Math.Round(Bet * Multiplier);

        var revealComponents = new ComponentBuilder();
        for (var i = 0; i < Math.Sqrt(Points.Count); i++)
        {
            var row = new ActionRowBuilder();
            for (var j = 0; j < Math.Sqrt(Points.Count); j++)
            {
                var tPonint = Points.Find(z => z.X == i && z.Y == j);
                row.AddComponent(new ButtonBuilder(tPonint!.Label, $"mine:{Id}:{i}:{j}", emote: tPonint.Emoji,
                    isDisabled: true).Build());
            }

            revealComponents.AddRow(row);
        }

        await Message.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder()
                .WithTitle($"Mines | {Id}")
                .WithColor(lost ? Color.Red : Color.Green)
                .WithDescription($"Bet: **{Bet}**\nMines: **{Mines}**\n" +
                                 (lost ? $"You lost **{Bet}**" : $"You won **{prize - Bet}** credits"))
                .Build();
            x.Components = revealComponents.Build();
        }).ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, prize, lost ? "MN - LOSE" : "MN - WIN", !lost));
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}

public class Point
{
    public Emoji Emoji;
    public bool IsClicked;
    public bool IsMine;
    public string Label;
    public int X;
    public int Y;
}