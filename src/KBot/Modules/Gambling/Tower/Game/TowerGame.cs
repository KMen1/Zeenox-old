using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Extensions;

namespace KBot.Modules.Gambling.Tower.Game;

public sealed class TowerGame : IGame
{
    public TowerGame(
        SocketGuildUser user,
        IUserMessage message,
        int bet,
        Difficulty difficulty)
    {
        Id = Guid.NewGuid().ToShortId();
        User = user;
        Message = message;
        Bet = bet;
        Difficulty = difficulty;
        Fields = new List<Field>();
        for (var x = 5; x > 0; x--)
        {
            var row = new List<Field>();
            for (var y = Columns; y > 0; y--)
                row.Add(new Field
                {
                    X = x,
                    Y = y,
                    IsMine = false,
                    Label = $"{Math.Round(Bet * x * Multiplier).ToString("N0", CultureInfo.InvariantCulture)}",
                    Prize = (int)Math.Round(Bet * x * Multiplier),
                    Emoji = new Emoji("🪙")
                });

            for (var i = 0; i < Mines; i++)
            {
                var index = RandomNumberGenerator.GetInt32(0, row.Count);
                while (row[index].IsMine) index = RandomNumberGenerator.GetInt32(0, row.Count);
                var orig = row[index];
                row[index] = orig with {Emoji = new Emoji("💣"), IsMine = true};
            }
            Fields.AddRange(row);
        }
    }

    public string Id { get; }
    public SocketGuildUser User { get; }
    private IUserMessage Message { get; }
    public int Bet { get; }
    public Difficulty Difficulty { get; }
    private int Columns => Difficulty is Difficulty.Medium ? 2 : 3;
    private int Mines => Difficulty is Difficulty.Hard ? 2 : 1;

    private double Multiplier => Difficulty switch
    {
        Difficulty.Easy => 1.455,
        Difficulty.Medium => 1.94,
        _ => 2.91
    };

    private List<Field> Fields { get; }
    private bool Lost { get; set; }
    private int Prize { get; set; }
    public event EventHandler<GameEndedEventArgs>? GameEnded;

    public Task StartAsync()
    {
        var comp = new ComponentBuilder();
        for (var i = 5; i > 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns; j > 0; j--)
            {
                var tPonint = Fields.Find(x => x.X == i && x.Y == j);
                row.AddComponent(new ButtonBuilder($"{tPonint.Label}$", $"towers:{Id}:{i}:{j}", emote: new Emoji("🪙"),
                    isDisabled: i != 1).Build());
            }

            comp.AddRow(row);
        }

        return Message.ModifyAsync(x =>
        {
            x.Content = "";
            x.Embed = new TowerEmbedBuilder(this, $"Exit: `/towers stop {Id}`").Build();
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
            return;
        }

        Prize = point.Prize;

        if (x == 5)
        {
            await Message.ModifyAsync(u => u.Embed = new TowerEmbedBuilder(this,
                    Lost
                        ? $"**Result:** You lost **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                        : $"**Result:** You won **{Prize.ToString("N0", CultureInfo.InvariantCulture)}** credits!")
                    .WithColor(Lost ? Color.Red : Color.Green)
                    .Build())
                .ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, User, Bet, Prize, "Towers: WIN", true));
        }
        
        var comp = new ComponentBuilder();
        var index = Fields.FindIndex(f => f.X == x);
        var orig = Fields[index];
        for (var i = 0; i < Columns; i++)
        {
            Fields[index + i] = orig with {Disabled = true, Y = orig.Y - i, Emoji = Fields[index + i].Emoji};
        }
        for (var i = 5; i > 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns; j > 0; j--)
            {
                var tPonint = Fields.Find(t => t.X == i && t.Y == j);
                row.AddComponent(tPonint!.Disabled
                    ? new ButtonBuilder($"{tPonint.Label}$", $"towers:{Id}:{i}:{j}", emote: tPonint.Emoji,
                        isDisabled: true).Build()
                    : new ButtonBuilder($"{tPonint.Label}$", $"towers:{Id}:{i}:{j}", emote: new Emoji("🪙"),
                        isDisabled: i > x + 1).Build());
            }

            comp.AddRow(row);
        }

        await Message.ModifyAsync(z => z.Components = comp.Build()).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        var prize = Lost ? 0 : Prize;
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

        await Message.ModifyAsync(x =>
        {
            x.Embed = new TowerEmbedBuilder(this,
                Lost
                    ? $"**Result:** You lost **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                    : $"**Result:** You won **{Prize.ToString("N0", CultureInfo.InvariantCulture)}** credits!")
                .WithColor(Lost ? Color.Red : Color.Green)
                .Build();
            x.Components = revealComponents.Build();
        }).ConfigureAwait(false);
        OnGameEnded(Lost
            ? new GameEndedEventArgs(Id, User, Bet, prize, "Towers: LOSE", false)
            : new GameEndedEventArgs(Id, User, Bet, prize, "Towers: WIN", true));
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}