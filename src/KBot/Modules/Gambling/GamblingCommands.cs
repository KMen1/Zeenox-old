using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling;

[Group("gamble", "Szerencsejáték")]
public class GamblingCommands : KBotModuleBase
{
    [SlashCommand("profile", "Szerencsejáték statjaid lekérése")]
    public async Task SendGamblingProfileAsync(SocketUser vuser = null)
    {
        var user = vuser ?? Context.User;
        if (user.IsBot)
        {
            await FollowupAsync("Bot profilját nem tudud lekérni.").ConfigureAwait(false);
        }
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        await RespondAsync(embed: dbUser.Gambling.ToEmbedBuilder(user).Build(), ephemeral:true).ConfigureAwait(false);
    }

    [SlashCommand("transactions", "Tranzakciók lekérése")]
    public async Task SendTransactionsAsync(SocketUser user = null)
    {
        var dbUser = await Database.GetUserAsync(Context.Guild, user ?? Context.User).ConfigureAwait(false);
        var transactions = dbUser.Transactions;
        var embeds = new List<Embed>();
        for (var i = 0; i < transactions.Count; i++)
        {
            i++;
            if (i % 1000 == 0)
            {
                embeds.Add(new EmbedBuilder()
                    .WithTitle($"{user?.Username ?? Context.User.Username} tranzakciói")
                    .WithColor(Color.Blue)
                    .WithDescription(
                        transactions.Count == 0 ? "Nincsenek tranzakciók" : string.Join("\n", transactions)).Build());
            }
        }
        await RespondAsync(embeds: embeds.ToArray(), ephemeral:true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changemoney", "Pénz addolása/csökkentése (admin)")]
    public async Task ChangeBalanceAsync(SocketUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        if (user.IsBot)
        {
            await FollowupAsync("Bot pénzét nem tudod változtatni.").ConfigureAwait(false);
        }
        var dbUser = await UpdateUserAsync(user, x =>
        {
            x.Gambling.Balance += offset;
            x.Transactions.Add(new Transaction("-", TransactionType.Correction, offset, $"{Context.User.Mention} által"));
        }).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Pénz beállítva!",
            $"{user.Mention} mostantól {dbUser.Gambling.Balance.ToString()} 🪙KCoin-al rendelkezik!").ConfigureAwait(false);
    }

    [SlashCommand("transfer", "Pénz küldése más személynek")]
    public async Task TransferBalanceAsync(SocketUser user, [MinValue(1)]int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var sourceUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (sourceUser.Gambling.Balance < amount)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ehhez a művelethez!").ConfigureAwait(false);
            return;
        }
        var fee = (int)Math.Round(amount * 0.10);
        await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= amount - fee;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferSend, -amount, $"Neki: {user.Mention}"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(user, x =>
        {
            x.Money += amount - fee;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferReceive, amount, $"Tőle: {Context.User.Mention}"));
        }).ConfigureAwait(false);
        await UpdateUserAsync(BotUser,x =>
        {
            x.Money += fee;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferFee, fee));
        }).ConfigureAwait(false);
        await FollowupAsync($"Sikeresen elküldtél {amount-fee}({amount}) KCoin-t {user.Mention} felhasználónak!").ConfigureAwait(false);
    }

    [SlashCommand("daily", "Napi bónusz KCoin begyűjtése")]
    public async Task ClaimDailyCoinsAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var lastDaily = dbUser.Gambling.DailyClaimDate;
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
            await FollowupWithEmbedAsync(Color.Green, "Sikeresen begyűjtetted a napi KCoin-od!", $"A begyűjtött KCoin mennyisége: {reward.ToString()}", ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
            await FollowupWithEmbedAsync(Color.Green, "Sikertelen begyűjtés",
                    $"Gyere vissza {timeLeft.Days.ToString()} nap, {timeLeft.Hours.ToString()} óra, {timeLeft.Minutes.ToString()} perc és {timeLeft.Seconds.ToString()} másodperc múlva!", ephemeral: true)
                .ConfigureAwait(false);
        }
    }
    
    [SlashCommand("remaining", "Nyerhető pénzmennyiség")]
    public async Task SendBudgetAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await GetDbUser(BotUser).ConfigureAwait(false);
        var embed = new EmbedBuilder()
            .WithTitle("Nyerhető pénzmennyiség")
            .WithDescription($"{dbUser.Money.ToString()} KCoin")
            .WithColor(Color.Gold);
        await FollowupAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    [SlashCommand("rob", "Pénzszállítás eltérítése")]
    public async Task RobAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        if (DateTime.Today.Day != 1)
        {
            await FollowupAsync().ConfigureAwait(false);
        }
    }
    
    [RequireOwner]
    [SlashCommand("refill", "Nyerhető pénzmennyiség beállítása")]
    public async Task SendBudgetAsync(int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await UpdateUserAsync(BotUser, x => x.Money = amount).ConfigureAwait(false);
        await FollowupAsync("Sikeresen feltöltötted a nyerhető pénzmennyiséget!").ConfigureAwait(false);
    }
}