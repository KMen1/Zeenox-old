using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using KBot.Extensions;
using KBot.Models;

namespace KBot.Modules.Gambling;

[Group("gamble", "A place to win big or lose big")]
public class GamblingCommands : SlashModuleBase
{
    [SlashCommand("profile", "Gets your gambling statistics")]
    public async Task SendGamblingProfileAsync(SocketUser? vuser = null)
    {
        var user = vuser ?? Context.User;
        if (user.IsBot) await FollowupAsync("You can't get a bot's profile.").ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser) user).ConfigureAwait(false);
        await RespondAsync(embed: dbUser.ToEmbedBuilder(user).Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("transactions", "Gets you transactions")]
    public async Task SendTransactionsAsync(SocketUser? user = null)
    {
        var transactions =
            (await Mongo.GetTransactionsAsync((SocketGuildUser) (user ?? Context.User)).ConfigureAwait(false)).ToList();
        if (transactions.Count == 0)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have any transactions**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var chunks = transactions.ChunkBy(500);

        var embeds = chunks.ToList().ConvertAll(chunk => new EmbedBuilder()
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
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Transaction not found**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Transaction - {transaction.Id}")
            .WithColor(Color.Blue)
            .AddField("Date", transaction.Date.ToString("G", CultureInfo.InvariantCulture), true)
            .AddField("Amount", $"`{transaction.Amount.ToString("N0", CultureInfo.InvariantCulture)}`", true)
            .AddField("Type", $"`{transaction.Source.GetDescription()}`", true);

        if (transaction.From is not null)
            embed.AddField("From", $"`{Context.Guild.GetUser((ulong) transaction.From).Mention}`", true);
        if (transaction.To is not null)
            embed.AddField("To", $"`{Context.Guild.GetUser((ulong) transaction.To).Mention}`", true);
        if (transaction.Description is not null)
            embed.AddField("Reason", $"```{transaction.Description}```");

        await RespondAsync(embed: embed.Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("transfer", "Sends money to another user")]
    public async Task TransferBalanceAsync(SocketGuildUser user, [MinValue(1)] [MaxValue(10000000)] int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var sourceUser = await Mongo.GetUserAsync((SocketGuildUser) Context.User).ConfigureAwait(false);
        if (sourceUser.Balance < amount)
        {
            var veb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Insufficient funds!**")
                .Build();
            await FollowupAsync(embed: veb).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToShortId();
        await Mongo.AddTransactionAsync(new Transaction(
            id,
            TransactionType.Transfer,
            amount,
            null,
            Context.User.Id,
            user.Id), null).ConfigureAwait(false);
        await Mongo.UpdateUserAsync((SocketGuildUser) Context.User, x =>
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
            .AddField("Amount", $"`{amount.ToString("N0", CultureInfo.InvariantCulture)}`", true)
            .AddField("To", $"{user.Mention}", true)
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    [SlashCommand("daily", "Collects you daily money")]
    public async Task ClaimDailyCoinsAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser) Context.User).ConfigureAwait(false);
        var lastDaily = dbUser.DailyBalanceClaim;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var id = Guid.NewGuid().ToShortId();
            var reward = RandomNumberGenerator.GetInt32(1000, 10000);
            await Mongo.UpdateUserAsync((SocketGuildUser) Context.User, x =>
            {
                x.DailyBalanceClaim = DateTime.UtcNow;
                x.Balance += reward;
                x.TransactionIds.Add(id);
            }).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green, $"Succesfully collected {reward} coins", "", ephemeral: true)
                .ConfigureAwait(false);
            return;
        }

        var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
        await FollowupWithEmbedAsync(Color.Green, "Unable to collect",
            $"Come back in {timeLeft.Humanize()}", ephemeral: true).ConfigureAwait(false);
    }
}

public class AdminCommands : SlashModuleBase
{
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("changebalance", "Change someones balance (admin)")]
    public async Task ChangeBalanceAsync(SocketGuildUser user, int offset, string reason)
    {
        if (user.IsBot)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You can't change a bot's balance.**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
        }

        await DeferAsync(true).ConfigureAwait(false);
        var id = Guid.NewGuid().ToShortId();
        await Mongo.AddTransactionAsync(new Transaction(
            id,
            TransactionType.Correction,
            offset,
            reason,
            Context.User.Id,
            user.Id), user).ConfigureAwait(false);
        var dbUser = await Mongo.UpdateUserAsync(user, x => x.Balance += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Money set!",
                $"{user.Mention} now has a balance of: **{dbUser.Balance.ToString(CultureInfo.InvariantCulture)}**")
            .ConfigureAwait(false);
    }
}