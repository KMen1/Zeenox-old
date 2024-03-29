﻿using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Zeenox.Enums;
using Zeenox.Models.Games;
using Zeenox.Services;

namespace Zeenox.Modules.Gambling.Games;

public sealed class Mines : IGame
{
    private readonly Field[,] _fields = new Field[5, 5];

    public Mines(IUserMessage message, ulong userId, int bet, int mines)
    {
        Message = message;
        UserId = userId;
        Bet = bet;
        MineAmount = mines;

        SetupGameField();
        SetupMines();
    }

    private IUserMessage Message { get; }
    private int Bet { get; }
    public bool CanStop { get; private set; }
    private int MineAmount { get; }
    private int Clicked { get; set; }
    private int Size { get; } = 5;

    private decimal Multiplier
    {
        get
        {
            //(25! (25 - b - s)!) / ((25 - b)! (25 - s)!) * .97
            var one = Factorial(25) * Factorial(25 - MineAmount - Clicked);
            var two = Factorial(25 - MineAmount) * Factorial(25 - Clicked);
            var t = one / two * 0.97;
            return Math.Round((decimal) t, 2);
        }
    }

    public ulong UserId { get; }

    public event AsyncEventHandler<GameEndEventArgs> GameEnded = null!;

    public Task StartAsync()
    {
        var eb = new EmbedBuilder()
            .WithTitle("Mines")
            .WithColor(Color.Gold)
            .WithDescription(
                $"**Bet:** {Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n**Mines:** {MineAmount}"
            )
            .Build();

        var componentBuilder = new ComponentBuilder();
        for (var x = 0; x < Size; x++)
        {
            var row = new ActionRowBuilder();
            for (var y = 0; y < Size; y++)
                row.AddComponent(
                    new ButtonBuilder(" ", $"mine:{x}:{y}", emote: new Emoji("🪙")).Build()
                );

            componentBuilder.AddRow(row);
        }

        return Message.ModifyAsync(
            x =>
            {
                x.Embed = eb;
                x.Components = componentBuilder.Build();
            }
        );
    }

    private void SetupGameField()
    {
        for (var x = 0; x < Size; x++)
        for (var y = 0; y < Size; y++)
            _fields[x, y] = new Field
            {
                Emoji = new Emoji("🪙"),
                IsClicked = false,
                IsMine = false,
                Label = " "
            };
    }

    private void SetupMines()
    {
        for (var i = 0; i < MineAmount; i++)
        {
            var x = RandomNumberGenerator.GetInt32(0, Size);
            var y = RandomNumberGenerator.GetInt32(0, Size);
            var field = _fields[x, y];
            if (field.IsMine)
            {
                i--;
                continue;
            }

            _fields[x, y] = field with {Emoji = new Emoji("💣"), IsMine = true};
        }
    }

    public async Task ClickFieldAsync(int x, int y)
    {
        CanStop = true;
        var field = _fields[x, y];
        if (field.IsMine)
        {
            await StopAsync(true).ConfigureAwait(false);
            await OnGameEnded(new GameEndEventArgs(UserId, Bet, 0, GameResult.Lose)).ConfigureAwait(false);
            return;
        }

        _fields[x, y] = field with {IsClicked = true, Label = $"{Multiplier}x"};
        Clicked++;

        if (Clicked == 25 - MineAmount)
        {
            await StopAsync(false).ConfigureAwait(false);
            await OnGameEnded(new GameEndEventArgs(UserId, Bet, (int) (Bet * Multiplier), GameResult.Win))
                .ConfigureAwait(false);
            return;
        }

        var comp = new ComponentBuilder();
        for (var i = 0; i < Size; i++)
        {
            var row = new ActionRowBuilder();
            for (var j = 0; j < Size; j++)
            {
                var tPonint = _fields[i, j];
                row.AddComponent(
                    new ButtonBuilder(
                        tPonint.Label,
                        $"mine:{i}:{j}",
                        emote: new Emoji("🪙"),
                        isDisabled: tPonint.IsClicked
                    ).Build()
                );
            }

            comp.AddRow(row);
        }

        await Message.ModifyAsync(z => z.Components = comp.Build()).ConfigureAwait(false);
    }

    private static double Factorial(int n)
    {
        var value = 1.0;
        for (var i = 1; i <= n; i++)
            value *= i;

        return value;
    }

    public async Task StopAsync(bool lost)
    {
        var prize = lost ? 0 : (int) Math.Round(Bet * Multiplier);

        var revealComponents = new ComponentBuilder();
        for (var i = 0; i < Size; i++)
        {
            var row = new ActionRowBuilder();
            for (var j = 0; j < Size; j++)
            {
                var tPonint = _fields[i, j];
                row.AddComponent(
                    new ButtonBuilder(
                        tPonint.Label,
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
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Mines")
                        .WithColor(lost ? Color.Red : Color.Green)
                        .WithDescription(
                            $"**Bet:** {Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n**Mines:** {MineAmount}\n"
                            + (
                                lost
                                    ? $"**Result:** You lose **{Bet.ToString("N0", CultureInfo.InvariantCulture)}** credits"
                                    : $"**Result:** You win **{prize.ToString("N0", CultureInfo.InvariantCulture)}** credits"
                            )
                        )
                        .Build();
                    x.Components = revealComponents.Build();
                }
            )
            .ConfigureAwait(false);
        await OnGameEnded(
            new GameEndEventArgs(UserId, Bet, prize, lost ? GameResult.Lose : GameResult.Win)
        ).ConfigureAwait(false);
    }

    private Task OnGameEnded(GameEndEventArgs e)
    {
        return GameEnded.Invoke(this, e);
    }
}