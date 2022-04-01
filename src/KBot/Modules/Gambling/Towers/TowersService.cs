using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.Towers;

public class TowersService
{
    private readonly List<TowersGame> Games = new();

    public TowersGame CreateGame(SocketUser user, IUserMessage message, int bet, Difficulty difficulty)
    {
        var game = new TowersGame(user, message, bet, difficulty, Games);
        Games.Add(game);
        return game;
    }

    public TowersGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }
}

public class TowersGame : IGamblingGame
{
    public string Id { get; }
    public SocketUser User { get; }
    private IUserMessage Message { get; }
    public int Bet { get; }
    public Difficulty Difficulty { get; }
    private int Columns => Difficulty is Difficulty.Medium ? 2 : 3;
    private int Mines => Difficulty is Difficulty.Hard ? 2 : 1;
    private double Multiplier => Difficulty is Difficulty.Easy ? 1.455 : Difficulty is Difficulty.Medium ? 1.94 : 2.91;
    private List<Field> Fields { get; }
    private List<TowersGame> Games { get; }
    private bool Lost { get; set; }
    private int Prize { get; set; }

    public TowersGame(SocketUser user, IUserMessage message, int bet, Difficulty difficulty, List<TowersGame> games)
    {
        Id = Guid.NewGuid().ConvertToGameId();
        User = user;
        Message = message;
        Bet = bet;
        Difficulty = difficulty;
        Games = games;
        Fields = new List<Field>();
        var rand = new Random();
        for (int x = 5; x > 0; x--)
        {
            var row = new List<Field>();
            for (int y = Columns; y > 0; y--)
            {
                row.Add(new Field { X = x, Y = y, IsMine = false, Label = $"{Math.Round(Bet*x*Multiplier)}", Emoji = new Emoji("🪙")});
            }

            while (row.Count(x => x.IsMine) < Mines)
            {
                var index = rand.Next(0, row.Count);
                row[index].IsMine = true;
                row[index].Emoji = new Emoji("💣");
            }
            Fields.AddRange(row);
        }
    }

    public Task StartAsync()
    {
        var comp = new ComponentBuilder();
        for (var i = 5; i > 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns; j > 0; j--)
            {
                var tPonint = Fields.Find(x => x.X == i && x.Y == j);
                row.AddComponent(new ButtonBuilder($"{tPonint.Label}$", $"towers:{Id}:{i}:{j}", emote: new Emoji("🪙"), isDisabled: i != 1).Build());
            }
            comp.AddRow(row);
        }
        return Message.ModifyAsync(x =>
        {
            x.Content = "";
            x.Embed = new EmbedBuilder().TowersEmbed(this, $"Kilépéshez: `/towers stop {Id}`");
            x.Components = comp.Build();
        });
    }
    
    public async Task ClickFieldAsync(int x, int y)
    {
        var point = Fields.Find(z => z.X == x && z.Y == y);
        if (point!.IsMine)
        {
            Lost = true;
            await StopAsync().ConfigureAwait(false);
            Games.Remove(this);
            return;
        }
        Prize = (int)Math.Round(Bet*x*Multiplier);
        var comp = new ComponentBuilder();
        Fields.Where(f => f.X == x).ToList().ForEach(f => f.Disabled = true);
        for (var i = 5; i > 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns; j > 0; j--)
            {
                var tPonint = Fields.Find(x => x.X == i && x.Y == j);
                row.AddComponent(tPonint!.Disabled
                    ? new ButtonBuilder($"{tPonint.Label}$", $"towers:{Id}:{i}:{j}", emote: tPonint.Emoji,
                        isDisabled: true).Build()
                    : new ButtonBuilder($"{tPonint.Label}$", $"towers:{Id}:{i}:{j}", emote: new Emoji("🪙"),
                        isDisabled: i > x+1).Build());
            }
            comp.AddRow(row);
        }
        await Message.ModifyAsync(x => x.Components = comp.Build()).ConfigureAwait(false);
        
    }

    public async Task<int> StopAsync()
    {
        var prize = Lost ? 0 : Prize;
        await RevealAsync().ConfigureAwait(false);
        await Message.ModifyAsync(x => x.Embed = new EmbedBuilder().TowersEmbed(this, $"Nyeremény: **{prize} KCoin**", Lost ? Color.Red : Color.Green)).ConfigureAwait(false);
        return prize;
    }

    private Task RevealAsync()
    {
        var revealComponents = new ComponentBuilder();
        for (var i = 5; i > 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns; j > 0; j--)
            {
                var tPonint = Fields.Find(z => z.X == i && z.Y == j);
                row.AddComponent(new ButtonBuilder($"{tPonint!.Label}$", $"mine:{Id}:{i}:{j}", emote: tPonint.Emoji,
                    isDisabled: true).Build());
            }

            revealComponents.AddRow(row);
        }

        return Message.ModifyAsync(x => x.Components = revealComponents.Build());
    }
}

public class Field
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsMine { get; set; }
    public Emoji Emoji { get; set; }
    public string Label { get; set; }
    public bool Disabled { get; set; }
}

public enum Difficulty
{
    [Description("Könnyű")]
    Easy = 1,
    [Description("Közepes")]
    Medium = 2,
    [Description("Nehéz")]
    Hard = 3
}