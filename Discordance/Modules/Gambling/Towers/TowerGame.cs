using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discordance.Enums;
using Discordance.Models;
using Discordance.Models.Games;
using Discordance.Modules.Gambling.Tower.Game;

namespace Discordance.Modules.Gambling.Towers;

public sealed class TowerGame : IGame
{
    private readonly Field[,] _fields;

    public TowerGame(ulong userId, IUserMessage message, int bet, Difficulty difficulty)
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

    public ulong UserId { get; }
    private IUserMessage Message { get; }
    public int Bet { get; }
    public Difficulty Difficulty { get; }
    private int Columns { get; }
    private int Mines { get; }
    private double Multiplier { get; }
    private bool Lost { get; set; }
    private int Prize { get; set; }
    public event EventHandler<GameEndEventArgs>? GameEnded;

    private void SetupGameField()
    {
        for (var x = 4; x >= 0; x--)
        {
            for (var y = Columns - 1; y >= 0; y--)
            {
                _fields[x, y] = new Field
                {
                    IsMine = false,
                    Label = $"{Math.Round(Bet * (x + 1) * Multiplier)}",
                    Emoji = new Emoji("🪙")
                };
            }
        }
    }

    private void SetupMines()
    {
        var random = new Random();
        for (var x = 4; x >= 0; x--)
        {
            for (var i = 0; i < Mines; i++)
            {
                var col = random.Next(0, Columns);
                var field = _fields[x, col];
                if (field.IsMine)
                {
                    i--;
                    continue;
                }

                _fields[x, col] = field with { Emoji = new Emoji("💣"), IsMine = true };
            }
        }
    }

    public Task StartAsync()
    {
        var comp = new ComponentBuilder();
        for (var x = 4; x >= 0; x--)
        {
            var row = new ActionRowBuilder();
            for (var y = Columns - 1; y >= 0; y--)
            {
                var tPonint = _fields[x, y];
                row.AddComponent(
                    new ButtonBuilder(
                        $"{tPonint.Label}$",
                        $"towers:{x}:{y}",
                        emote: new Emoji("🪙"),
                        isDisabled: x != 0
                    ).Build()
                );
            }
            comp.AddRow(row);
        }

        return Message.ModifyAsync(
            x =>
            {
                x.Content = "";
                x.Embed = new TowerEmbedBuilder(this).Build();
                x.Components = comp.Build();
            }
        );
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
            await Message
                .ModifyAsync(
                    u =>
                        u.Embed = new TowerEmbedBuilder(
                            this,
                            Lost
                              ? $"**Result:** You lost **{Bet:N0}** credits!"
                              : $"**Result:** You won **{Prize:N0}** credits!"
                        )
                            .WithColor(Lost ? Color.Red : Color.Green)
                            .Build()
                )
                .ConfigureAwait(false);
            OnGameEnded(new GameEndEventArgs(UserId, Bet, Prize, GameResult.Win));
        }

        var comp = new ComponentBuilder();
        var orig = _fields[x, 0];
        for (var i = Columns - 1; i >= 0; i--)
        {
            _fields[x, i] = orig with { Disabled = true, Emoji = _fields[x, i].Emoji };
        }
        for (var i = 4; i >= 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns - 1; j >= 0; j--)
            {
                var tPonint = _fields[i, j];
                row.AddComponent(
                    tPonint.Disabled
                      ? new ButtonBuilder(
                            $"{tPonint.Label}$",
                            $"towers:{i}:{j}",
                            emote: tPonint.Emoji,
                            isDisabled: true
                        ).Build()
                      : new ButtonBuilder(
                            $"{tPonint.Label}$",
                            $"towers:{i}:{j}",
                            emote: new Emoji("🪙"),
                            isDisabled: i > x + 1
                        ).Build()
                );
            }

            comp.AddRow(row);
        }

        await Message.ModifyAsync(z => z.Components = comp.Build()).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        var prize = Lost ? 0 : Prize;
        var revealComponents = new ComponentBuilder();
        for (var i = 4; i >= 0; i--)
        {
            var row = new ActionRowBuilder();
            for (var j = Columns - 1; j >= 0; j--)
            {
                var tPonint = _fields[i, j];
                row.AddComponent(
                    new ButtonBuilder(
                        $"{tPonint.Label}$",
                        $"mine:{i}:{j}",
                        emote: tPonint.Emoji,
                        isDisabled: true
                    ).Build()
                );
            }

            revealComponents.AddRow(row);
        }

        await Message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new TowerEmbedBuilder(
                        this,
                        Lost
                          ? $"**Result:** You lost **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                          : $"**Result:** You won **{Prize.ToString("N0", CultureInfo.InvariantCulture)}** credits!"
                    )
                        .WithColor(Lost ? Color.Red : Color.Green)
                        .Build();
                    x.Components = revealComponents.Build();
                }
            )
            .ConfigureAwait(false);
        OnGameEnded(
            Lost
              ? new GameEndEventArgs(UserId, Bet, prize, GameResult.Lose)
              : new GameEndEventArgs(UserId, Bet, prize, GameResult.Win)
        );
    }

    private void OnGameEnded(GameEndEventArgs e)
    {
        GameEnded?.Invoke(this, e);
    }
}
