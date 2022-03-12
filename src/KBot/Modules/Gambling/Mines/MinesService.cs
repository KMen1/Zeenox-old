using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Services;

namespace KBot.Modules.Gambling.Mines;

public class MinesService
{
    private readonly DatabaseService Database;
    private readonly List<MinesGame> Games = new();

    public MinesService(DatabaseService database)
    {
        Database = database;
    }

    public MinesGame CreateGame(SocketUser user, IUserMessage message, int bet, int size, int mines)
    {
        var game = new MinesGame(Games, CreateId(), message, user, bet, size, mines);
        Games.Add(game);
        return game;
    }

    public MinesGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }

    private static string CreateId()
    {
        var ticks = new DateTime(2016, 1, 1).Ticks;
        var ans = DateTime.Now.Ticks - ticks;
        return ans.ToString("x");
    }
}

public class MinesGame
{
    public string Id { get; }
    private readonly List<Point> Points = new();
    private IUserMessage Message { get; }
    public SocketUser User { get; }
    private int Bet { get; set; }
    private bool Lost { get; set; }
    public bool CanStop { get; set; }
    private int Mines => Points.Count(x => x.IsMine);
    private int Clicked => Points.Count(x => x.IsClicked && !x.IsMine);
    private List<MinesGame> Container { get; }
    

    private decimal Multiplier
    {
        get
        {
            var szam = Factorial(25 - Mines) * Factorial(25 - Clicked);
            var oszt = Factorial(25) * Factorial(25 - Mines - Clicked);
            var t = (decimal)Math.Round(szam / oszt, 2);
            return Math.Round((decimal).97 * (1 / t), 2);
        }
    }
        
    public MinesGame(List<MinesGame> container, string id, IUserMessage message, SocketUser user, int bet, int size, int mines)
    {
        Id = id;
        Message = message;
        User = user;
        Container = container;
        Bet = bet;
        var Random = new Random();
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                Points.Add(new Point { X = i, Y = j, Emoji = new Emoji("🪙"), IsClicked = false, Label = " "});
            }
        }
        
        for (var i = 0; i < mines ; i++)
        {
            var index = Random.Next(0, Points.Count);
            while (Points[index].IsMine)
            {
                index = Random.Next(0, Points.Count);
            }
            Points[index].IsMine = true;
            Points[index].Emoji = new Emoji("💣");
        }
    }

    public Task StartAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle("Mines")
            .WithColor(Color.Gold)
            .WithDescription($"Tét: **{Bet}**\nAknák száma: **{Mines}**\nKilépéshez: `/mine stop {Id}`")
            .Build();
        var comp = new ComponentBuilder();
        var size = Math.Sqrt(Points.Count);
        for (var x = 0; x < size; x++)
        {
            var row = new ActionRowBuilder();
            for (var y = 0; y < size; y++)
            {
                row.AddComponent(new ButtonBuilder(" ", $"mine:{Id}:{x}:{y}", emote: new Emoji("🪙")).Build());
            }

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
            Lost = true;
            await StopAsync().ConfigureAwait(false);
            Container.Remove(this);
            return;
        }
        point!.IsClicked = true;
        point.Label = $"{Multiplier}x";
        Bet = (int)(Bet * Multiplier);
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
        await Message.ModifyAsync(x => x.Components = comp.Build()).ConfigureAwait(false);
    }

    private Task RevealAsync()
    {
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

        return Message.ModifyAsync(x => x.Components = revealComponents.Build());
    }

    private static double Factorial(int n)
    {
        if (n == 0)
            return 1;
        return n * Factorial(n - 1);
    }

    public async Task<int?> StopAsync()
    {
        var prize = Lost ? 0 : (Bet * Multiplier) - Bet;
        var eb = new EmbedBuilder()
            .WithTitle("Mines")
            .WithColor(Color.Gold)
            .WithDescription($"Tét: **{Bet}**\nAknák száma: **{Mines}**\nNyeremény: **{Math.Round(prize)} KCoin**")
            .Build();
        await RevealAsync().ConfigureAwait(false);
        await Message.ModifyAsync(x => x.Embed = eb).ConfigureAwait(false);
        return Lost ? null : Bet;
    }
}
public class Point
{
    public int X;
    public int Y;
    public bool IsMine;
    public bool IsClicked;
    public Emoji Emoji;
    public string Label;
}