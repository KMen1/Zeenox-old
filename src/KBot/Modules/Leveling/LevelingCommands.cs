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
        var dbUser = await Database.GetUserAsync(Context.Guild, setUser).ConfigureAwait(false);
        var level = dbUser.Level;
        var requiredXP = Math.Pow(level * 4, 2);
        var xp = dbUser.Points;
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
        var top = await Database.GetTopAsync(Context.Guild.Id, 10).ConfigureAwait(false);

        var userColumn = "";
        var levelColumn = "";

        foreach (var user in top)
        {
            userColumn += $"{top.IndexOf(user) +1 }. {Context.Guild.GetUser(user.UserId).Mention}\n";
            levelColumn += $"{user.Level} ({user.Points} XP)\n";
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
        var lastDaily = dbUser.LastDailyClaim;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var xp = new Random().Next(100, 500);
            dbUser.LastDailyClaim = DateTime.UtcNow;
            dbUser.Points += xp;
            await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
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
    [SlashCommand("changepoints", "XP hozzáadása/csökkentése (admin)")]
    public async Task AddPointsAsync(SocketUser user, int xp)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        dbUser.Points += xp;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Pontok beállítva!",
            $"{user.Mention} mostantól {dbUser.Points.ToString()} XP-vel rendelkezik!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changelevel", "Szint hozzáadása/csökkentése (admin)")]
    public async Task AddLevelAsync(SocketUser user, int levels)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        dbUser.Level += levels;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Szint hozzáadva!",
            $"{user.Mention} mostantól {dbUser.Level.ToString()} szintű!").ConfigureAwait(false);
    }
}