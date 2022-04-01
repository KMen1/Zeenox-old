using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.Mines;

public class MinesService
{
    private readonly List<MinesGame> Games = new();

    public MinesGame CreateGame(SocketUser user, IUserMessage message, int bet, int size, int mines)
    {
        var game = new MinesGame(message, user, bet, size, mines, Games);
        Games.Add(game);
        return game;
    }

    public MinesGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }
}

public class MinesGame : IGamblingGame
{
    public string Id { get; }
    private readonly List<Point> Points = new();
    private IUserMessage Message { get; }
    public SocketUser User { get; }
    public int Bet { get; }
    private bool Lost { get; set; }
    public bool CanStop { get; private set; }
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
        
    public MinesGame(
        IUserMessage message,
        SocketUser user,
        int bet,
        int size,
        int mines,
        List<MinesGame> container)
    {
        Id = Guid.NewGuid().ConvertToGameId();
        Message = message;
        User = user;
        Container = container;
        Bet = bet;
        var random = new Random();
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                Points.Add(new Point { X = i, Y = j, Emoji = new Emoji("🪙"), IsClicked = false, Label = " "});
            }
        }
        
        for (var i = 0; i < mines ; i++)
        {
            var index = random.Next(0, Points.Count);
            while (Points[index].IsMine)
            {
                index = random.Next(0, Points.Count);
            }
            Points[index].IsMine = true;
            Points[index].Emoji = new Emoji("💣");
        }
    }

    public Task StartAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle($"Mines | {Id}")
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
            .WithTitle($"Mines | {Id}")
            .WithColor(Color.Gold)
            .WithDescription($"Tét: **{Bet}**\nAknák száma: **{Mines}**\nNyeremény: **{Math.Round(prize)} KCoin**")
            .Build();
        
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
            x.Embed = eb;
            x.Components = revealComponents.Build();
        }).ConfigureAwait(false);
        return Lost ? null : (int)Math.Round(Bet * Multiplier);
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