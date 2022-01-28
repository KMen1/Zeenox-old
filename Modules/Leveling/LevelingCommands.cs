using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;

namespace KBot.Modules.Leveling;

[Group("level", "Szintrendszer parancsok")]
public class Levels : KBotModuleBase
{
    public DatabaseService Database { get; set; }

    [RequireOwner]
    [SlashCommand("registerguild", "Regisztrálja a szerverre a botot")]
    public async Task RegisterGuildAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await Database.RegisterGuildAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("Kész").ConfigureAwait(false);
    }

    [SlashCommand("rank", "Saját/más szint és xp lekérése")]
    public async Task GetLevelAsync(SocketUser user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var setUser = user ?? Context.User;
        var userId = setUser.Id;

        var level = await Database.GetUserLevelByIdAsync(Context.Guild.Id, userId).ConfigureAwait(false);
        var xp = await Database.GetUserPointsByIdAsync(Context.Guild.Id, userId).ConfigureAwait(false);

        var embed = new EmbedBuilder()
            .WithAuthor(setUser.Username, setUser.GetAvatarUrl())
            .WithColor(Color.Gold)
            .WithDescription(
                $"**XP: **`{xp.ToString()}/18000` ({(level * 18000 + xp).ToString()} Összesen) \n**Szint: **`{level.ToString()}`")
            .Build();

        await FollowupAsync(embed: embed).ConfigureAwait(false);
    }

    [SlashCommand("top", "Top 10 szintjei")]
    public async Task GetTopAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var top = await Database.GetTopAsync(Context.Guild.Id, 10).ConfigureAwait(false);

        await FollowupWithEmbedAsync(EmbedResult.Success, "Top 10 Rank",
                string.Join("\n", top.Select(x => $"{top.IndexOf(x) + 1 }. {Context.Guild.GetUser(x.UserId).Mention} - `Lvl. {x.Level} ({x.Points} XP)`")))
            .ConfigureAwait(false);
    }

    [SlashCommand("daily", "Napi XP begyűjtése")]
    public async Task GetDailyAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var userId = Context.User.Id;
        var lastDaily = await Database.GetDailyClaimDateByIdAsync(Context.Guild.Id, userId).ConfigureAwait(false);
        var canClaim = lastDaily.AddDays(1) < DateTime.Now;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var xp = new Random().Next(100, 500);
            await Database.SetDailyClaimDateByIdAsync(Context.Guild.Id, userId, DateTime.Now).ConfigureAwait(false);
            await Database.AddPointsByUserIdAsync(Context.Guild.Id, userId, xp).ConfigureAwait(false);
            await FollowupWithEmbedAsync(EmbedResult.Success, "Sikeresen begyűjtetted a napi XP-d!", $"A begyűjtött XP mennyisége: {xp.ToString()}").ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.Now;
            await FollowupWithEmbedAsync(EmbedResult.Error, "Sikertelen begyűjtés",
                    $"Gyere vissza {timeLeft.Days.ToString()} nap, {timeLeft.Hours.ToString()} óra, {timeLeft.Minutes.ToString()} perc és {timeLeft.Seconds.ToString()} másodperc múlva!")
                .ConfigureAwait(false);
        }
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("setpoints", "XP állítása (admin)")]
    public async Task SetPointsAsync(SocketUser user, int points)
    {
        await DeferAsync().ConfigureAwait(false);
        var newPoints = await Database.SetPointsByUserIdAsync(Context.Guild.Id, user.Id, points).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Pontok hozzáadva!",
            $"{user.Mention} mostantól {newPoints.ToString()} XP-vel rendelkezik!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("addpoints", "XP hozzáadása (admin)")]
    public async Task AddPointsAsync(SocketUser user, int pointsToAdd)
    {
        await DeferAsync().ConfigureAwait(false);
        var points = await Database.AddPointsByUserIdAsync(Context.Guild.Id, user.Id, pointsToAdd).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Pontok beállítva!",
            $"{user.Mention} mostantól {points.ToString()} XP-vel rendelkezik!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("addlevel", "Szint hozzáadása (admin)")]
    public async Task AddLevelAsync(SocketUser user, int levelsToAdd)
    {
        await DeferAsync().ConfigureAwait(false);
        var level = await Database.AddLevelByUserIdAsync(Context.Guild.Id, user.Id, levelsToAdd).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!",
            $"{user.Mention} mostantól {level.ToString()} szintű!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("setlevel", "Szint állítása (admin)")]
    public async Task SetLevelAsync(SocketUser user, int level)
    {
        await DeferAsync().ConfigureAwait(false);
        var newLevel = await Database.SetLevelByUserIdAsync(Context.Guild.Id, user.Id, level).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!",
            $"{user.Mention} mostantól {newLevel.ToString()} szintű!").ConfigureAwait(false);
    }
}