using System;
using System.Collections.Generic;
using System.Linq;
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
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        await RespondAsync(embed: dbUser.Gambling.ToEmbedBuilder().Build(), ephemeral:true).ConfigureAwait(false);
    }

    [SlashCommand("transactions", "Tranzakciók lekérése")]
    public async Task SendTransactionsAsync(SocketUser user = null)
    {
        var dbUser = await Database.GetUserAsync(Context.Guild, user ?? Context.User).ConfigureAwait(false);
        var transactions = dbUser.Transactions;
        var embed = new EmbedBuilder()
            .WithTitle($"{user?.Username ?? Context.User.Username} tranzakciói")
            .WithColor(Color.Blue)
            .WithDescription(transactions.Count == 0 ? "Nincsenek tranzakciók" : string.Join("\n\n", transactions));
        await RespondAsync(embed: embed.Build(), ephemeral:true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changebalance", "Pénz addolása/csökkentése (admin)")]
    public async Task ChangeBalanceAsync(SocketUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
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
        
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.Balance -= amount;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferSend, amount, $"Neki: {user.Mention}"));
        }).ConfigureAwait(false);
        await Database.UpdateUserAsync(Context.Guild, user, x =>
        {
            x.Gambling.Balance += amount;
            x.Transactions.Add(new Transaction("-", TransactionType.TransferReceive, amount, $"Tőle: {Context.User.Mention}"));
        }).ConfigureAwait(false);
        
        await FollowupAsync($"Sikeresen elküldtél {amount} 🪙KCoin-t {user.Mention} felhasználónak!").ConfigureAwait(false);
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
            var Balance = new Random().Next(1000, 10000);
            await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
            {
                x.Gambling.DailyClaimDate = DateTime.UtcNow;
                x.Gambling.Balance += Balance;
                x.Transactions.Add(new Transaction("-", TransactionType.DailyClaim, Balance));
            }).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green, "Sikeresen begyűjtetted a napi KCoin-od!", $"A begyűjtött KCoin mennyisége: {Balance.ToString()}", ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
            await FollowupWithEmbedAsync(Color.Green, "Sikertelen begyűjtés",
                    $"Gyere vissza {timeLeft.Days.ToString()} nap, {timeLeft.Hours.ToString()} óra, {timeLeft.Minutes.ToString()} perc és {timeLeft.Seconds.ToString()} másodperc múlva!", ephemeral: true)
                .ConfigureAwait(false);
        }
    }

    [RequireOwner]
    [SlashCommand("reset", "Szerencsejáték statisztikák törlése (admin)")]
    public async Task Reset()
    {
        await DeferAsync().ConfigureAwait(false);
        await Database.Update(Context.Guild).ConfigureAwait(false);
        await FollowupAsync("Kész").ConfigureAwait(false);
    }
}

[Group("shop", "Szerencsejáték piac")]
public class Shop : KBotModuleBase
{
    [SlashCommand("level", "Szint vásárlása")]
    public async Task BuyLevelAsync([MinValue(1)] int levels)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var req = dbUser.MoneyToBuyLevel(levels);
        if (dbUser.Money < req)
        {
            await FollowupAsync($"Nincs elég pénzed! (Kell még: {req})").ConfigureAwait(false);
            return;
        }

        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Money -= req;
            x.Level += levels;
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -req, $"+{levels} szint"));
        }).ConfigureAwait(false);
        await FollowupAsync($"Sikeres vásárlás! (Költség: {req})").ConfigureAwait(false);
    }

    [SlashCommand("role", "Rang vásárlása")]
    public async Task BuyRoleAsync(string name, uint hexcolor)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Money < 10000000)
        {
            await FollowupAsync($"Nincs elég pénzed! (Kell még: {10000000 - dbUser.Money})").ConfigureAwait(false);
            return;
        }
        
        var role = await Context.Guild.CreateRoleAsync(name, GuildPermissions.None, new Color(hexcolor)).ConfigureAwait(false);
        await ((SocketGuildUser) Context.User).AddRoleAsync(role).ConfigureAwait(false);
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Money -= 10000000;
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{role.Mention} rang"));
            x.BoughtRoles.Add(role.Id);
        }).ConfigureAwait(false);
        await FollowupAsync($"Sikeres vásárlás! (Költség: {10000000})").ConfigureAwait(false);
    }
    
    [SlashCommand("category", "Kategória vásárlása")]
    public async Task BuyCategoryAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Money < 10000000)
        {
            await FollowupAsync($"Nincs elég pénzed! (Kell még: {10000000 - dbUser.Money})").ConfigureAwait(false);
            return;
        }

        if (dbUser.BoughtChannels.Exists(x => x.Type == DiscordChannelType.Category))
        {
            await FollowupAsync("Már van saját kategóriád.").ConfigureAwait(false);
            return;
        }

        var category = await Context.Guild.CreateCategoryChannelAsync(name, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
        }).ConfigureAwait(false);
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Money -= 10000000;
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{category.Name} kategória"));
            x.BoughtChannels.Add(new DiscordChannel(category.Id, DiscordChannelType.Category));
        }).ConfigureAwait(false);
        await FollowupAsync($"Sikeres vásárlás! (Költség: {10000000})").ConfigureAwait(false);
    }

    [SlashCommand("textchannel", "Szövegcsatorna vásárlása")]
    public async Task BuyChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Money < 10000000)
        {
            await FollowupAsync($"Nincs elég pénzed! (Kell még: {10000000 - dbUser.Money})").ConfigureAwait(false);
            return;
        }
        var category = dbUser.BoughtChannels.Find(x => x.Type == DiscordChannelType.Category);
        if (category == null)
        {
            await FollowupAsync("Nincs saját kategóriád.").ConfigureAwait(false);
            return;
        }

        var channel = await Context.Guild.CreateTextChannelAsync(name, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
            x.CategoryId = category.Id;
        }).ConfigureAwait(false);
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Money -= 10000000;
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{channel.Mention} szövegcsatorna"));
            x.BoughtChannels.Add(new DiscordChannel(channel.Id, DiscordChannelType.Text));
        }).ConfigureAwait(false);
        await FollowupAsync($"Sikeres vásárlás! (Költség: {10000000})").ConfigureAwait(false);
    }
    
    [SlashCommand("voicechannel", "Hangcsatorna vásárlása")]
    public async Task BuyVoiceChannelAsync(string name)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Money < 10000000)
        {
            await FollowupAsync($"Nincs elég pénzed! (Kell még: {10000000 - dbUser.Money})").ConfigureAwait(false);
            return;
        }
        var category = dbUser.BoughtChannels.Find(x => x.Type == DiscordChannelType.Category);
        if (category == null)
        {
            await FollowupAsync("Nincs saját kategóriád.").ConfigureAwait(false);
            return;
        }

        var channel = await Context.Guild.CreateVoiceChannelAsync(name, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
            x.CategoryId = category.Id;
        }).ConfigureAwait(false);
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Money -= 10000000;
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -1000000, $"{channel.Mention} hangcsatorna"));
            x.BoughtChannels.Add(new DiscordChannel(channel.Id, DiscordChannelType.Voice));
        }).ConfigureAwait(false);
        await FollowupAsync($"Sikeres vásárlás! (Költség: {10000000})").ConfigureAwait(false);
    }
    
    [SlashCommand("ultimate", "Minden vásárlása")]
    public async Task BuyUltimateAsync([MinValue(1)]int levels, string roleName, uint roleColor, string categoryName, string textName, string voiceName)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var req = dbUser.MoneyToBuyLevel(levels) + 50000000;
        if (dbUser.Money < req)
        {
            await FollowupAsync($"Nincs elég pénzed! (Kell még: {req - dbUser.Money})").ConfigureAwait(false);
            return;
        }

        if (dbUser.BoughtChannels.Exists(x => x.Type == DiscordChannelType.Category))
        {
            await FollowupAsync("Már vásároltál kategóriát ezért nem válthatod be a packot.").ConfigureAwait(false);
        }

        var category = await Context.Guild.CreateCategoryChannelAsync(categoryName, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
        }).ConfigureAwait(false);

        var role = await Context.Guild.CreateRoleAsync(roleName, GuildPermissions.None, new Color(roleColor)).ConfigureAwait(false);
        await ((SocketGuildUser) Context.User).AddRoleAsync(role).ConfigureAwait(false);

        var text = await Context.Guild.CreateTextChannelAsync(textName, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
            x.CategoryId = category.Id;
        }).ConfigureAwait(false);

        var voice = await Context.Guild.CreateVoiceChannelAsync(voiceName, x =>
        {
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
            {
                new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),
                new Overwrite(Context.User.Id, PermissionTarget.User,
                    new OverwritePermissions(manageRoles: PermValue.Allow, viewChannel: PermValue.Allow))
            });
            x.CategoryId = category.Id;
        }).ConfigureAwait(false);

        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Money -= req;
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -15000000, $"+{levels} szint"));
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -15000000, $"{role.Mention} rang"));
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -25000000, $"{category.Name} kategória"));
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -50000000, $"{text.Mention} szövegcsatorna"));
            x.Transactions.Add(new Transaction("-", TransactionType.ShopPurchase, -50000000, $"{voice.Mention} hangcsatorna"));
            x.BoughtRoles.Add(role.Id);
            x.BoughtChannels.Add(new DiscordChannel(category.Id, DiscordChannelType.Category));
            x.BoughtChannels.Add(new DiscordChannel(text.Id, DiscordChannelType.Text));
            x.BoughtChannels.Add(new DiscordChannel(voice.Id, DiscordChannelType.Voice));
        }).ConfigureAwait(false);
    }
}