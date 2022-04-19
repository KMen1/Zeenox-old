using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using KBot.Extensions;
using StackExchange.Redis.KeyspaceIsolation;

namespace KBot.Modules.Gambling;

[Group("gamble", "A place to win big or lose big")]
public class GamblingCommands : SlashModuleBase
{
    [SlashCommand("profile", "Gets your gambling statistics")]
    public async Task SendGamblingProfileAsync(SocketUser vuser = null)
    {
        var user = vuser ?? Context.User;
        if (user.IsBot) await FollowupAsync("You can't get a bot's profile.").ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)user).ConfigureAwait(false);
        await RespondAsync(embed: dbUser.ToEmbedBuilder(user).Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("transactions", "Gets you transactions")]
    public async Task SendTransactionsAsync(SocketUser user = null)
    {
        var transactions = await Mongo.GetTransactionsAsync((SocketGuildUser)(user ?? Context.User)).ConfigureAwait(false);
        if (transactions.Count == 0)
        {
            await RespondAsync("You have no transactions.", ephemeral: true).ConfigureAwait(false);
            return;
        }
        var chunks = transactions.ChunkBy(500);

        var embeds = chunks.ConvertAll(chunk => new EmbedBuilder()
            .WithTitle($"{(user ?? Context.User).Username}'s transactions")
            .WithDescription("To get more information on a transaction use **/gamble transaction <id>**")
            .WithColor(Color.Blue)
            .WithDescription(string.Join("\n", chunk))
            .Build());
        await RespondAsync(embeds: embeds.ToArray(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("transaction", "Get more info on a specific transaction")]
    public async Task SendTransactionAsync(string id)
    {
        var transaction = await Mongo.GetTransactionAsync(id).ConfigureAwait(false);
        if (transaction is null)
        {
            await RespondAsync("Transaction not found.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Transaction - {transaction.Id}")
            .WithColor(Color.Blue)
            .AddField("Date", transaction.Date.ToString("G"), true)
            .AddField("Amount", $"`{transaction.Amount.ToString("N1")}`", true)
            .AddField("Type", $"`{transaction.Source.GetDescription()}`", true)
            .AddField("Reason", $"```{transaction.Description}```")
            .Build();

        await RespondAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changemoney", "Change someones balance (admin)")]
    public async Task ChangeBalanceAsync(SocketGuildUser user, int offset, string reason)
    {
        await DeferAsync(true).ConfigureAwait(false);
        if (user.IsBot) await FollowupAsync("You can't change a bot's balance.").ConfigureAwait(false);
        var id = Guid.NewGuid().ToShortId();
        var dbUser = await Mongo.UpdateUserAsync(user, x =>
        {
            x.Balance += offset;
            x.TransactionIds.Add(id);
        }).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Money set!",
            $"{user.Mention} now has a balance of: **{dbUser.Balance.ToString()}**").ConfigureAwait(false);
    }

    [SlashCommand("transfer", "Sends money to another user")]
    public async Task TransferBalanceAsync(SocketGuildUser user, [MinValue(1)] int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var sourceUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (sourceUser.Balance < amount)
        {
            await FollowupAsync("Insufficient funds!").ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();
        await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x =>
        {
            x.Balance -= amount;
            x.TransactionIds.Add(id);
        }).ConfigureAwait(false);
        await Mongo.UpdateUserAsync(user, x =>
        {
            x.Balance += amount;
            x.TransactionIds.Add(id);
        }).ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle("Transfer successful!")
            .WithColor(Color.Green)
            .AddField("Amount", $"`{amount}`", true)
            .AddField("To", $"{user.Mention}", true)
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    [SlashCommand("daily", "Collects you daily money")]
    public async Task ClaimDailyCoinsAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        var lastDaily = dbUser.DailyBalanceClaim;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var id = Guid.NewGuid().ToShortId();
            var reward = new Random().Next(1000, 10000);
            await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x =>
            {
                x.DailyBalanceClaim = DateTime.UtcNow;
                x.Balance += reward;
                x.TransactionIds.Add(id);
            }).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green, $"Succesfully collected {reward} coins", "", ephemeral: true)
                .ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
            await FollowupWithEmbedAsync(Color.Green, "Unable to collect",
                    $"Come back in {timeLeft.Humanize()}", ephemeral: true)
                .ConfigureAwait(false);
        }
    }
}