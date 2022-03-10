using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Gambling;

[Group("gamble", "Szerencsejáték")]
public class GamblingCommands : KBotModuleBase
{
    [SlashCommand("profile", "Szerencsejáték statjaid lekérése")]
    public async Task SendGamblingProfileAsync(GambleProfileType profileType = GambleProfileType.General, SocketUser vuser = null)
    {
        var user = vuser ?? Context.User;
        var dbUser = await Database.GetUserAsync(Context.Guild.Id, user.Id).ConfigureAwait(false);
        var gambleProfile = dbUser.GamblingProfile;

        switch (profileType)
        {
            case GambleProfileType.General:
            {
                await RespondAsync(embed: gambleProfile.ToEmbedBuilder().Build()).ConfigureAwait(false);
                break;
            }
            case GambleProfileType.HighLow:
            {
                await RespondAsync(embed: gambleProfile.HighLow.ToEmbedBuilder().Build()).ConfigureAwait(false);
                break;
            }
            case GambleProfileType.BlackJack:
            {
                await RespondAsync(embed: gambleProfile.BlackJack.ToEmbedBuilder().Build()).ConfigureAwait(false);
                break;
            }
            case GambleProfileType.Crash:
            {
                await RespondAsync(embed: gambleProfile.Crash.ToEmbedBuilder().Build()).ConfigureAwait(false);
                break;
            }
        }
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changemoney", "Pénz addolása/csökkentése (admin)")]
    public async Task ChangeMoneyAsync(SocketUser user, int offset)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild.Id, user.Id).ConfigureAwait(false);
        dbUser.GamblingProfile.Money += offset;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Pénz beállítva!",
            $"{user.Mention} mostantól {dbUser.GamblingProfile.Money.ToString()} 🪙KCoin-al rendelkezik!").ConfigureAwait(false);
    }

    [SlashCommand("transfer", "Pénz átadása (szerencsejáték)")]
    public async Task TrasnferMoneyAsync(SocketUser user, int amount)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var sourceUser = await Database.GetUserAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
        if (sourceUser.GamblingProfile.Money < amount)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ehhez a művelethez!").ConfigureAwait(false);
            return;
        }
        var destUser = await Database.GetUserAsync(Context.Guild.Id, user.Id).ConfigureAwait(false);
        sourceUser.GamblingProfile.Money -= amount;
        destUser.GamblingProfile.Money += amount;
        await Database.UpdateUserAsync(Context.Guild.Id, sourceUser).ConfigureAwait(false);
        await Database.UpdateUserAsync(Context.Guild.Id, destUser).ConfigureAwait(false);
        await FollowupAsync($"Sikeresen elküldtél {amount} 🪙KCoin-t {user.Mention} felhasználónak!").ConfigureAwait(false);

        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithTitle("🪙KCoin-t kaptál!")
            .WithDescription($"{Context.User.Mention} {amount} 🪙KCoin-t küldött neked!")
            .Build();
        await channel.SendMessageAsync(embed: eb).ConfigureAwait(false);
    }

    [SlashCommand("daily", "Napi bónusz KCoin begyűjtése")]
    public async Task ClaimDailyCoinsAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var userId = Context.User.Id;
        var dbUser = await Database.GetUserAsync(Context.Guild.Id, userId).ConfigureAwait(false);
        var lastDaily = dbUser.GamblingProfile.LastDailyClaim;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var money = new Random().Next(100, 500);
            dbUser.GamblingProfile.LastDailyClaim = DateTime.UtcNow;
            dbUser.GamblingProfile.Money += money;
            await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green, "Sikeresen begyűjtetted a napi KCoin-od!", $"A begyűjtött KCoin mennyisége: {money.ToString()}", ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
            await FollowupWithEmbedAsync(Color.Green, "Sikertelen begyűjtés",
                    $"Gyere vissza {timeLeft.Days.ToString()} nap, {timeLeft.Hours.ToString()} óra, {timeLeft.Minutes.ToString()} perc és {timeLeft.Seconds.ToString()} másodperc múlva!", ephemeral: true)
                .ConfigureAwait(false);
        }
    }

    /*[SlashCommand("update", "asa")]
    public async Task Update()
    {
        await DeferAsync();
        await Database.Update(Context.Guild);
        await FollowupAsync("kesz");
    }*/
    public enum GambleProfileType
    {
        General,
        HighLow,
        BlackJack,
        Crash
    }
}