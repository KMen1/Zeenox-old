using System;
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
    
    [SlashCommand("shop", "Szerencsejáték piac")]
    public async Task SendGambleShopAsync()
    {
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithTitle("Szerencsejáték piac")
            .WithDescription($"Kérlek válassz az alábbi lehetőségek közül!\nAz árat a kiválasztás után láthatod!\nElérhető egyenleg: **{dbUser.Gambling.Balance}** 🪙KCoin")
            .WithColor(Color.Gold)
            .Build();
        var selectMenu = new SelectMenuBuilder()
                .WithCustomId($"gamble-shop:{Context.User.Id}")
                .WithPlaceholder("Kérlek válassz!")
                .WithMinValues(1)
                .WithMaxValues(6)
                //.AddOption("Lottószelvény", "lottery", "Egy lottószelvény", new Emoji("🎟️"))
                .AddOption("+1 Szint", "PlusOneLevel", "Egy szint a szintrendszerben", new Emoji("1️⃣"))
                .AddOption("+10 Szint", "PlusTenLevel", "Tíz szint kedvezőbb áron.", new Emoji("🔟"))
                .AddOption("Saját rang", "OwnRank","Egy saját rang általad választott névvel és színnel.", new Emoji("🏆"));
        if (!dbUser.BoughtChannels.Exists(x => x.Type == DiscordChannelType.Category))
        {
            selectMenu.AddOption("Saját kategória", "OwnCategory",
                "Egy saját kategória (csak egyszer megvehető).", new Emoji("🔠"));
        }

        selectMenu.AddOption("Saját hangcsatorna", "OwnVoiceChannel",
                "Általad választott névvel és teljes hozzáféréssel.", new Emoji("🎤"))
            .AddOption("Saját szövegcsatorna", "OwnTextChannel",
                "Általad választott névvel és teljes hozzáféréssel.", new Emoji("💬"))
            .AddOption("Full Csomag", "All",
                "Magába foglalja az összes fentebbi dolgot.", new Emoji("📦"));
        await RespondAsync(embed: eb, components: new ComponentBuilder().WithSelectMenu(selectMenu).Build()).ConfigureAwait(false);
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