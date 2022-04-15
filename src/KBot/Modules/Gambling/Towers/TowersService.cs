using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Extensions;
using KBot.Models.User;
using KBot.Services;

namespace KBot.Modules.Gambling.Towers;

public class TowersService : IInjectable
{
    private readonly DatabaseService Database;
    private readonly List<TowersGame> Games = new();

    public TowersService(DatabaseService database)
    {
        Database = database;
    }

    public TowersGame CreateGame(SocketUser user, IUserMessage message, int bet, Difficulty difficulty)
    {
        var game = new TowersGame(user, message, bet, difficulty);
        Games.Add(game);
        game.GameEnded += HandleGameEndedAsync;
        return game;
    }

    private async void HandleGameEndedAsync(object sender, GameEndedEventArgs e)
    {
        var game = (TowersGame) sender!;
        game.GameEnded -= HandleGameEndedAsync;
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
        if (e.IsWin) await Database.UpdateBotUserAsync(game.Guild, x => x.Money -= e.Prize).ConfigureAwait(false);
    }

    public TowersGame GetGame(string id)
    {
        return Games.Find(x => x.Id == id);
    }
}

public sealed class TowersGame : IGamblingGame
{
    public TowersGame(
        SocketUser user,
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
        var rand = new Random();
        for (var x = 5; x > 0; x--)
        {
            var row = new List<Field>();
            for (var y = Columns; y > 0; y--)
                row.Add(new Field
                {
                    X = x, Y = y, IsMine = false, Label = $"{Math.Round(Bet * x * Multiplier)}", Emoji = new Emoji("🪙")
                });

            while (row.Count(z => z.IsMine) < Mines)
            {
                var index = rand.Next(0, row.Count);
                row[index].IsMine = true;
                row[index].Emoji = new Emoji("💣");
            }

            Fields.AddRange(row);
        }
    }

    public string Id { get; }
    public SocketUser User { get; }
    private IUserMessage Message { get; }
    public IGuild Guild => ((ITextChannel) Message.Channel).Guild;
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
    public event EventHandler<GameEndedEventArgs> GameEnded;

    public Task StartAsync()
    {
        var comp = new ComponentBuilder();
        for (var i = 5; i > 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns; j > 0; j--)
            {
                var tPonint = Fields.Find(x => x.X == i && x.Y == j);
                row.AddComponent(new ButtonBuilder($"{tPonint?.Label}$", $"towers:{Id}:{i}:{j}", emote: new Emoji("🪙"),
                    isDisabled: i != 1).Build());
            }

            comp.AddRow(row);
        }

        return Message.ModifyAsync(x =>
        {
            x.Content = "";
            x.Embed = new EmbedBuilder().TowersEmbed(this, $"Exit: `/towers stop {Id}`");
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

        Prize = (int) Math.Round(Bet * Multiplier);

        if (x == 5)
        {
            await Message.ModifyAsync(u => u.Embed = new EmbedBuilder().TowersEmbed(this,
                    Lost ? $"You lost **{Bet}** credits" : $"You won **{Prize}** credits",
                    Lost ? Color.Red : Color.Green))
                .ConfigureAwait(false);
            OnGameEnded(new GameEndedEventArgs(Id, Prize, "TW - Win", true));
        }

        var comp = new ComponentBuilder();
        Fields.Where(f => f.X == x).ToList().ForEach(f => f.Disabled = true);
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

    public async Task<int> StopAsync()
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
            x.Embed = new EmbedBuilder().TowersEmbed(this,
                Lost ? $"You lost **{Bet}** credits" : $"You won **{Prize}** credits",
                Lost ? Color.Red : Color.Green);
            x.Components = revealComponents.Build();
        }).ConfigureAwait(false);
        OnGameEnded(new GameEndedEventArgs(Id, prize, "TW - Lose", false));
        return prize;
    }

    private void OnGameEnded(GameEndedEventArgs e)
    {
        GameEnded?.Invoke(this, e);
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
    [Description("Easy")] Easy = 1,
    [Description("Medium")] Medium = 2,
    [Description("Hard")] Hard = 3
}