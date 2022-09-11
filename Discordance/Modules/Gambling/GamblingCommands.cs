using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Modules.Gambling.Mines;
using Discordance.Modules.Gambling.Tower.Game;
using Discordance.Modules.Gambling.Towers;
using Discordance.Services;
using Humanizer;

namespace Discordance.Modules.Gambling;

public class GamblingCommands : GambleBase
{
    [SlashCommand("gamble-start", "Starts a new game")]
    public async Task StartGameAsync(
        [Summary("Game")] GameType gameType,
        [MinValue(1)] [MaxValue(10000000)] int bet,
        [Summary("MineAmount", "This setting only affects mines")] int mines = 5,
        [Summary("Difficulty", "This setting only affects towers")]
            Difficulty difficulty = Difficulty.Easy
    )
    {
        var dbUser = await DatabaseService.GetUserAsync(Context.User.Id).ConfigureAwait(false);
        var result = dbUser.CanStartGame(bet, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var sEb = new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithDescription("**Starting Game...**")
            .Build();
        await RespondAsync(embed: sEb).ConfigureAwait(false);
        var msg = await GetOriginalResponseAsync().ConfigureAwait(true);
        if (
            !GameService.TryStartGame(
                Context.User.Id,
                msg,
                gameType,
                bet,
                out var game,
                mines,
                difficulty
            )
        )
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("**You are already in a game!**")
                        .Build(),
                    ephemeral: true
                )
                .ConfigureAwait(false);
            await msg.DeleteAsync().ConfigureAwait(false);
            return;
        }
        await game!.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("gamble-stop", "Stops the game you're currently playing")]
    public async Task StopGameAsync()
    {
        if (!GameService.TryGetGame(Context.User.Id, out var generic))
        {
            await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("**You are currently not playing!**")
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        if (!generic.CanAffectGame(Context.User.Id, out var feb))
        {
            await RespondAsync(embed: feb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        if (generic is not MinesGame or TowerGame)
        {
            await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("**You can only stop `mines` or `towers` games!**")
                        .Build(),
                    ephemeral: true
                )
                .ConfigureAwait(false);
        }

        switch (generic)
        {
            case MinesGame { CanStop: false }:
            {
                var sEb = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription(
                        "**You need to click at least one field to be able to stop the game.**"
                    )
                    .Build();
                await RespondAsync(embed: sEb, ephemeral: true).ConfigureAwait(false);
                return;
            }
            case MinesGame minesGame:
                await minesGame.StopAsync(false).ConfigureAwait(false);
                break;
            case TowerGame towerGame:
                await towerGame.StopAsync().ConfigureAwait(false);
                break;
        }

        await DeferAsync(true).ConfigureAwait(false);
        var stopEb = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription("**Stopped.**")
            .Build();
        await FollowupAsync(embed: stopEb).ConfigureAwait(false);
    }

    [SlashCommand("gamble-transfer", "Sends money to another user")]
    public async Task TransferBalanceAsync(
        SocketGuildUser user,
        [MinValue(1)] [MaxValue(10000000)] int amount
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var sourceUser = await DatabaseService.GetUserAsync(Context.User.Id).ConfigureAwait(false);
        if (sourceUser.Balance < amount)
        {
            var veb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Insufficient funds!**")
                .Build();
            await FollowupAsync(embed: veb).ConfigureAwait(false);
            return;
        }

        await DatabaseService
            .UpdateUserAsync(sourceUser.Id, x => x.Balance -= amount)
            .ConfigureAwait(false);
        await DatabaseService
            .UpdateUserAsync(user.Id, x => x.Balance += amount)
            .ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithDescription("Transfer successful!")
            .WithColor(Color.Green)
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    [SlashCommand("gamble-daily", "Collects you daily money")]
    public async Task ClaimDailyCoinsAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await DatabaseService.GetUserAsync(Context.User.Id).ConfigureAwait(false);
        var lastDaily = dbUser.LastDailyCreditClaim;
        if (!lastDaily.HasValue || lastDaily.Value.AddDays(1) < DateTime.UtcNow)
        {
            var reward = RandomNumberGenerator.GetInt32(1000, 10000);
            await DatabaseService
                .UpdateUserAsync(
                    dbUser.Id,
                    x =>
                    {
                        x.LastDailyCreditClaim = DateTime.UtcNow;
                        x.Balance += reward;
                    }
                )
                .ConfigureAwait(false);
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle($"Succesfully collected {reward} coins")
                        .Build(),
                    ephemeral: true
                )
                .ConfigureAwait(false);
            return;
        }

        var timeLeft = lastDaily.Value.AddDays(1) - DateTime.UtcNow;
        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("Unable to collect")
                    .WithDescription($"Come back in {timeLeft.Humanize()}")
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }
}
