using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using KBot.Extensions;
using KBot.Models.User;

namespace KBot.Modules.Gambling;

[Group("gamble", "A place to win big or lose big")]
public class GamblingCommands : SlashModuleBase
{
    [SlashCommand("profile", "Gets your gambling statistics")]
    public async Task SendGamblingProfileAsync(SocketUser vuser = null)
    {
        var user = vuser ?? Context.User;
        if (user.IsBot) await FollowupAsync("You can't get a bot's profile.").ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        await RespondAsync(embed: dbUser.Gambling.ToEmbedBuilder(user).Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("transactions", "Gets you transactions")]
    public async Task SendTransactionsAsync(SocketUser user = null)
    {
        var dbUser = await Database.GetUserAsync(Context.Guild, user ?? Context.User).ConfigureAwait(false);
        var transactions = dbUser.Transactions;
        var embeds = new List<Embed>();
        for (var i = 0; i < transactions.Count; i++)
        {
            i++;
            if (i % 1000 == 0)
                embeds.Add(new EmbedBuilder()
                    .WithTitle($"{user?.Username ?? Context.User.Username}'s transactions")
                    .WithColor(Color.Blue)
                    .WithDescription(
                        transactions.Count == 0 ? "No transactions yet." : string.Join("\n", transactions)).Build());
        }

        await RespondAsync(embeds: embeds.ToArray(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("transaction", "Get more info on a specific transaction")]
    public async Task SendTransactionAsync(string id)
    {
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var transaction = dbUser.Transactions.Find(x => x.Id == id);
        if (transaction == null)
        {
            await RespondAsync("Transaction not found.").ConfigureAwait(false);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Transaction - {transaction.Id}")
            .WithColor(Color.Blue)
            .AddField("Date", transaction.Date.ToString("G"), true)
            .AddField("Amount", $"`{transaction.Amount:C}`", true)
            .AddField("Type", $"`{transaction.Type.GetDescription()}`", true)
            .AddField("Reason", $"```{transaction.Description}```")
            .Build();

        await RespondAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changemoney", "Change someones balance (admin)")]
    public async Task ChangeBalanceAsync(SocketUser user, int offset, string reason)
    {
        await DeferAsync(true).ConfigureAwait(false);
        if (user.IsBot) await FollowupAsync("You can't change a bot's balance.").ConfigureAwait(false);
        var dbUser = await UpdateUserAsync(user, x =>
        {
            x.Gambling.Balance += offset;
            x.Transactions.Add(new Transaction("-", TransactionType.Correction, offset,
                $"{Context.User.Mention}: {reason}"));
        }).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Money set!",
            $"{user.Mention} now has a balance of: **{dbUser.Gambling.Balance.ToString()}**").ConfigureAwait(false);
    }

    [SlashCommand("transfer", "Sends money to another user")]
    public async Task TransferBalanceAsync(SocketUser user, [MinValue(1)] int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var sourceUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (sourceUser.Gambling.Balance < amount)
        {
            await FollowupAsync("Insufficient funds!").ConfigureAwait(false);
            return;
        }

        var fee = (int) Math.Round(amount * 0.10);
        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= amount - fee;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferSend, -amount, $"To: {user.Mention}"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(user, x =>
        {
            x.Money += amount - fee;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferReceive, amount,
                $"From: {Context.User.Mention}"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x =>
        {
            x.Money += fee;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferFee, fee));
        }).ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle("Transfer successful!")
            .WithColor(Color.Green)
            .AddField("Amount", $"`{amount}`", true)
            .AddField("Fee", $"`{fee}`", true)
            .AddField("To", $"{user.Mention}", true)
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    [SlashCommand("daily", "Collects you daily money")]
    public async Task ClaimDailyCoinsAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var lastDaily = dbUser.Gambling.DailyClaimDate ?? DateTime.MinValue;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var reward = new Random().Next(1000, 10000);
            await UpdateUserAsync(Context.User, x =>
            {
                x.Gambling.DailyClaimDate = DateTime.UtcNow;
                x.Gambling.Balance += reward;
                x.Transactions.Add(new Transaction("-", TransactionType.DailyClaim, reward));
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

    [SlashCommand("remaining", "Gets the available money the guild has")]
    public async Task SendBudgetAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await GetDbUser(BotUser).ConfigureAwait(false);
        var embed = new EmbedBuilder()
            .WithTitle("Guild balance")
            .WithDescription($"**{dbUser.Money.ToString()}**")
            .WithColor(Color.Gold);
        await FollowupAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    /*[SlashCommand("rob", "Pénzszállítás eltérítése")]
    public async Task RobAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        if (DateTime.Today.Day != 1)
            await FollowupWithEmbedAsync(Color.Red, "Sikertelen eltérítés",
                "Pénzszállítás csak a hónap első napján van").ConfigureAwait(false);
    }*/

    [RequireOwner]
    [SlashCommand("refill", "Refill guild balance")]
    public async Task SendBudgetAsync(int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money = amount).ConfigureAwait(false);
        await FollowupAsync("Succesfully set available guild balance!").ConfigureAwait(false);
    }
}