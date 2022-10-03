using System;
using System.Threading.Tasks;
using Discord;
using Discordance.Enums;
using Discordance.Models;
using Discordance.Models.Games;
using Discordance.Modules.Gambling.Tower.Game;

namespace Discordance.Modules.Gambling.Games;

public sealed class Towers : IGame
{
    private readonly Field[,] _fields;

    public Towers(ulong userId, IUserMessage message, int bet, Difficulty difficulty)
    {
        UserId = userId;
        Message = message;
        Bet = bet;
        Difficulty = difficulty;
        Mines = difficulty is Difficulty.Hard ? 2 : 1;
        Columns = difficulty is Difficulty.Medium ? 2 : 3;
        Multiplier = difficulty switch
        {
            Difficulty.Easy => 1.455,
            Difficulty.Medium => 1.94,
            _ => 2.91
        };
        _fields = new Field[5, Columns];

        SetupGameField();
        SetupMines();
    }

    private IUserMessage Message { get; }
    public int Bet { get; }
    public Difficulty Difficulty { get; }
    private int Columns { get; }
    private int Mines { get; }
    private double Multiplier { get; }
    private bool Lost { get; set; }
    private int Prize { get; set; }

    public ulong UserId { get; }
    public event EventHandler<GameEndEventArgs>? GameEnded;

    public Task StartAsync()
    {
        return UpdateMessageAsync(start: true);
    }

    private void SetupGameField()
    {
        for (var x = 4; x >= 0; x--)
        for (var y = Columns - 1; y >= 0; y--)
            _fields[x, y] = new Field
            {
                IsMine = false,
                Label = $"{Math.Round(Bet * (x + 1) * Multiplier)}",
                Emoji = new Emoji("🪙")
            };
    }

    private void SetupMines()
    {
        var random = new Random();
        for (var x = 4; x >= 0; x--)
        for (var i = 0; i < Mines; i++)
        {
            var col = random.Next(0, Columns);
            var field = _fields[x, col];
            if (field.IsMine)
            {
                i--;
                continue;
            }

            _fields[x, col] = field with {Emoji = new Emoji("💣"), IsMine = true};
        }
    }

    private MessageComponent CreateComponents(bool start, int clickedRow, bool reveal)
    {
        var comp = new ComponentBuilder();

        for (var x = 4; x >= 0; x--)
        {
            var row = new ActionRowBuilder();
            for (var y = Columns - 1; y >= 0; y--)
            {
                var field = _fields[x, y];
                row.AddComponent(
                    new ButtonBuilder(
                        $"{field.Label}$",
                        $"towers:{x}:{y}",
                        emote: field.Disabled
                            ? field.Emoji
                            : reveal
                                ? field.Emoji
                                : new Emoji("🪙"),
                        isDisabled: reveal
                                    || (start ? x != 0 : field.Disabled || x > clickedRow + 1)
                    ).Build()
                );
            }

            comp.AddRow(row);
        }

        return comp.Build();
    }

    public async Task ClickFieldAsync(int x, int y)
    {
        var field = _fields[x, y];
        if (field.IsMine)
        {
            Lost = true;
            await StopAsync().ConfigureAwait(false);
            return;
        }

        Prize = int.Parse(field.Label);

        if (x == 4)
        {
            await UpdateMessageAsync($"**Result:** You won **{Prize:N0}** credits!", reveal: true)
                .ConfigureAwait(false);
            OnGameEnded(new GameEndEventArgs(UserId, Bet, Prize, GameResult.Win));
        }

        var firstFieldInRow = _fields[x, 0];
        for (var i = Columns - 1; i >= 0; i--)
            _fields[x, i] = firstFieldInRow with {Disabled = true, Emoji = _fields[x, i].Emoji};

        await UpdateMessageAsync(clickedRow: x).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        var prize = Lost ? Bet : Prize;
        await UpdateMessageAsync(
                $"**Result:** You {(Lost ? "lost" : "won")} **{prize:N0}** credits!",
                reveal: true
            )
            .ConfigureAwait(false);
        OnGameEnded(
            Lost
                ? new GameEndEventArgs(UserId, Bet, prize, GameResult.Lose)
                : new GameEndEventArgs(UserId, Bet, prize, GameResult.Win)
        );
    }

    private Task UpdateMessageAsync(
        string? desc = null,
        bool start = false,
        int clickedRow = 0,
        bool reveal = false
    )
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Towers")
            .WithColor(Color.Gold)
            .WithDescription(
                $"**Bet:** {Bet:N0} credits\n"
                + $"**Difficulty:** {Difficulty.ToString()}"
                + (desc is null ? "" : $"\n{desc}")
            );

        if (desc is not null)
            return Message.ModifyAsync(
                x =>
                {
                    x.Embed = embedBuilder.Build();
                    x.Components = CreateComponents(false, clickedRow, true);
                }
            );

        if (!reveal)
            return Message.ModifyAsync(
                x =>
                {
                    x.Embed = embedBuilder.Build();
                    x.Components = CreateComponents(start, clickedRow, false);
                }
            );

        return Message.ModifyAsync(
            x =>
            {
                x.Embed = embedBuilder.Build();
                x.Components = CreateComponents(false, clickedRow, true);
            }
        );
    }

    private void OnGameEnded(GameEndEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}