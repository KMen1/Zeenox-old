using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Leveling;

[Group("level", "Szintrendszer parancsok")]
public class Levels : KBotModuleBase
{
    [SlashCommand("rank", "Saját/más szint és xp lekérése")]
    public async Task GetLevelAsync(SocketUser user = null)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var setUser = user ?? Context.User;
        if (setUser.IsBot)
        {
            await FollowupAsync("Botok szintjét nem tudod lekérdezni.").ConfigureAwait(false);
            return;
        }
        var dbUser = await Database.GetUserAsync(Context.Guild, setUser).ConfigureAwait(false);
        var level = dbUser.Level;
        var requiredXP = Math.Pow(level * 4, 2);
        var xp = dbUser.XP;
        var embed = new EmbedBuilder()
            .WithAuthor(setUser.Username, setUser.GetAvatarUrl())
            .WithColor(Color.Gold)
            .WithDescription(
                $"**XP: **`{xp.ToString()}/{requiredXP}` ({dbUser.TotalXp} Összesen) \n**Szint: **`{level.ToString()}`")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("top", "Top 10 szintjei")]
    public async Task GetTopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var top = await Database.GetTopOsuPlayersInGuildAsync(Context.Guild, 10).ConfigureAwait(false);

        var userColumn = "";
        var levelColumn = "";

        foreach (var user in top)
        {
            userColumn += $"{top.IndexOf(user) +1 }. {Context.Guild.GetUser(user.Id).Mention}\n";
            levelColumn += $"{user.Level} ({user.XP} XP)\n";
        }

        await FollowupAsync(embed: new EmbedBuilder()
            .WithTitle("Top 10 szintjei")
            .WithColor(Color.Green)
            .AddField("Felhasználó", userColumn, true)
            .AddField("Szint", levelColumn, true)
            .Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("daily", "Napi XP begyűjtése")]
    public async Task GetDailyAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var lastDaily = dbUser.DailyClaimDate;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var xp = new Random().Next(100, 500);
            await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
            {
                x.DailyClaimDate = DateTime.UtcNow;
                x.XP += xp;
            }).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green, "Sikeresen begyűjtetted a napi XP-d!", $"A begyűjtött XP mennyisége: {xp.ToString()}", ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
            await FollowupWithEmbedAsync(Color.Green, "Sikertelen begyűjtés",
                    $"Gyere vissza {timeLeft.Days.ToString()} nap, {timeLeft.Hours.ToString()} óra, {timeLeft.Minutes.ToString()} perc és {timeLeft.Seconds.ToString()} másodperc múlva!", ephemeral: true)
                .ConfigureAwait(false);
        }
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changexp", "XP hozzáadása/csökkentése (admin)")]
    public async Task ChangeXPAsync(SocketUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.UpdateUserAsync(Context.Guild, user, x => x.XP += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Pontok beállítva!",
            $"{user.Mention} mostantól {dbUser.XP.ToString()} XP-vel rendelkezik!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changelevel", "Szint hozzáadása/csökkentése (admin)")]
    public async Task ChangeLevelAsync(SocketUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.UpdateUserAsync(Context.Guild, user, x => x.Level += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Szint hozzáadva!",
            $"{user.Mention} mostantól {dbUser.Level.ToString()} szintű!").ConfigureAwait(false);
    }
}