using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Modules.Gambling.Games;
using Discordance.Preconditions;
using Humanizer;

namespace Discordance.Modules.Gambling;

public class Commands : GambleBase
{
    [SlashCommand("gamble-start", "Starts a new game")]
    public async Task StartGameAsync(
        [Summary("Game")] GameType gameType,
        [MinValue(1)] [MaxValue(10000000)] int bet,
        [Summary("MineAmount", "This setting only affects mines")]
        int mines = 5,
        [Summary("Difficulty", "This setting only affects towers")]
        Difficulty difficulty = Difficulty.Easy
    )
    {
        var dbUser = await GetUserAsync().ConfigureAwait(false);
        var result = dbUser.CanStartGame(bet, out var eb);
        if (!result)
        {
            await RespondAsync(ephemeral: true, embed: eb).ConfigureAwait(false);
            return;
        }

        var sEb = new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithDescription("**Starting Game...**")
            .Build();
        await RespondAsync(embed: sEb).ConfigureAwait(false);
        var msg = await GetOriginalResponseAsync().ConfigureAwait(false);
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
                    ephemeral: true,
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("**You are already in a game!**")
                        .Build())
                .ConfigureAwait(false);
            await msg.DeleteAsync().ConfigureAwait(false);
            return;
        }

        await game!.StartAsync().ConfigureAwait(false);
    }

    [RequireActiveGame]
    [SlashCommand("gamble-stop", "Stops the game you're currently playing")]
    public async Task StopGameAsync()
    {
        var game = GetGame();

        if (game is not Mines or Towers)
            await RespondAsync(
                    ephemeral: true,
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("**You can only stop `mines` or `towers` games!**")
                        .Build())
                .ConfigureAwait(false);

        switch (game)
        {
            case Mines {CanStop: false}:
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
            case Mines minesGame:
                await minesGame.StopAsync(false).ConfigureAwait(false);
                break;
            case Towers towerGame:
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
        [Summary("target", "The person you want to send the money to")]
        SocketGuildUser user,
        [Summary("amount", "The amount of money you want to send")] [MinValue(1)] [MaxValue(10000000)]
        int amount
    )
    {
        await DeferAsync(true).ConfigureAwait(false);
        var sourceUser = await GetUserAsync().ConfigureAwait(false);
        if (sourceUser.Balance < amount)
        {
            var veb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Insufficient funds!**")
                .Build();
            await FollowupAsync(embed: veb).ConfigureAwait(false);
            return;
        }

        await UpdateUserAsync(x => x.Balance -= amount).ConfigureAwait(false);
        await UpdateUserAsync(x => x.Balance += amount, user.Id).ConfigureAwait(false);

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
        var dbUser = await GetUserAsync().ConfigureAwait(false);
        var lastDaily = dbUser.LastDailyCreditClaim;
        if (!lastDaily.HasValue || lastDaily.Value.AddDays(1) < DateTime.UtcNow)
        {
            var reward = RandomNumberGenerator.GetInt32(1000, 10000);
            await UpdateUserAsync(
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