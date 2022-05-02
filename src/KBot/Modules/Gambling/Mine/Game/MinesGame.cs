using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Extensions;

namespace KBot.Modules.Gambling.Mine.Game;

public sealed class MinesGame : IGame
{
    private readonly List<Field> _points = new();

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
        Mines = mines;
        for (var i = 0; i < 5; i++)
        {
            for (var j = 0; j < 5; j++)
            {
                _points.Add(new Field
                {
                    Emoji = new Emoji("🪙"),
                    IsClicked = false,
                    IsMine = false,
                    Label = " ",
                    X = i,
                    Y = j
                });
            }
        }

        for (var i = 0; i < mines; i++)
        {
            var index = RandomNumberGenerator.GetInt32(0, _points.Count);
            while (_points[index].IsMine) index = RandomNumberGenerator.GetInt32(0, _points.Count);
            var orig = _points[index];
            _points[index] = orig with {Emoji = new Emoji("💣"), IsMine = true, Label = " "};
        }
    }

    public string Id { get; }
    private IUserMessage Message { get; }
    public SocketGuildUser User { get; }
    public int Bet { get; }
    public bool CanStop { get; private set; }
    private int Mines { get; }
    private int Clicked { get; set; }

    private decimal Multiplier
    {
        get
        {
            //(25! (25 - b - s)!) / ((25 - b)! (25 - s)!) * .97
            var one = Factorial(25) * Factorial(25 - Mines - Clicked);
            var two = Factorial(25 - Mines) * Factorial(25 - Clicked);
            var t = one / two * 0.97;
            return Math.Round((decimal) t, 2);
        }
    }

    public event EventHandler<GameEndedEventArgs>? GameEnded;

    public Task StartAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle($"Mines | {Id}")
            .WithColor(Color.Gold)
            .WithDescription(
                $"**Bet:** {Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n**Mines:** {Mines}\n**Exit:** `/mine stop {Id}`")
            .Build();
        var comp = new ComponentBuilder();
        var size = Math.Sqrt(_points.Count);
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
        var index = _points.FindIndex(point => point.X == x && point.Y == y);
        var orig = _points[index];
        if (orig.IsMine)
        {
            await StopAsync(true).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, User, Bet, 0, "Mines: LOSE", false));
            return;
        }

        Clicked++;
        _points[index] = orig with {IsClicked = true, Label = $"{Multiplier}x"};
        if (!_points.Any(u => !u.IsClicked && !u.IsMine))
        {
            await StopAsync(false).ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, User, Bet, (int) (Bet * Multiplier), "Mines: WIN", false));
            return;
        }

        var comp = new ComponentBuilder();
        for (var i = 0; i < Math.Sqrt(_points.Count); i++)
        {
            var row = new ActionRowBuilder();
            for (var j = 0; j < Math.Sqrt(_points.Count); j++)
            {
                var tPonint = _points.Find(z => z.X == i && z.Y == j);
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
        for (var i = 0; i < Math.Sqrt(_points.Count); i++)
        {
            var row = new ActionRowBuilder();
            for (var j = 0; j < Math.Sqrt(_points.Count); j++)
            {
                var tPonint = _points.Find(z => z.X == i && z.Y == j);
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
                .WithDescription(
                    $"**Bet:** {Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n**Mines:** {Mines}\n" +
                    (lost
                        ? $"**Result:** You lose **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits"
                        : $"**Result:** You win **{prize.ToString("N0", CultureInfo.InvariantCulture)}** credits"))
                .Build();
            x.Components = revealComponents.Build();
        }).ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, User, Bet, prize, lost ? "Mines: LOSE" : "Mines: WIN", !lost));
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}