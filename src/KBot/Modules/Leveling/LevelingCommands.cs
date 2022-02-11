using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Services;

namespace KBot.Modules.Leveling;

[Group("level", "Szintrendszer parancsok")]
public class Levels : KBotModuleBase
{
    public DatabaseService Database { get; set; }

    [SlashCommand("rank", "Saját/más szint és xp lekérése")]
    public async Task GetLevelAsync(SocketUser user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var setUser = user ?? Context.User;
        var userId = setUser.Id;

        var level = await Database.GetLevelAsync(Context.Guild.Id, userId).ConfigureAwait(false);
        var xp = await Database.GetPointsAsync(Context.Guild.Id, userId).ConfigureAwait(false);
        
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

        var userColumn = "";
        var levelColumn = "";

        foreach (var user in top)
        {
            userColumn += $"{top.IndexOf(user) +1 }. {Context.Guild.GetUser(user.UserId).Mention}\n";
            levelColumn += $"`{user.Level} ({user.Points} XP)`\n";
        }

        await FollowupAsync(embed: new EmbedBuilder()
            .WithTitle("Top 10 szintjei")
            .WithColor(Color.Green)
            .AddField("Felhasználó", userColumn, true)
            .AddField("Szint", levelColumn, true)
            .Build()).ConfigureAwait(false);
    }

    [SlashCommand("daily", "Napi XP begyűjtése")]
    public async Task GetDailyAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var userId = Context.User.Id;
        var lastDaily = await Database.GetDailyClaimDateAsync(Context.Guild.Id, userId).ConfigureAwait(false);
        var canClaim = lastDaily.AddDays(1) < DateTime.Now;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var xp = new Random().Next(100, 500);
            await Database.SetDailyClaimDateAsync(Context.Guild.Id, userId, DateTime.Now).ConfigureAwait(false);
            await Database.AddPointsAsync(Context.Guild.Id, userId, xp).ConfigureAwait(false);
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
        var newPoints = await Database.SetPointsAsync(Context.Guild.Id, user.Id, points).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Pontok hozzáadva!",
            $"{user.Mention} mostantól {newPoints.ToString()} XP-vel rendelkezik!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("addpoints", "XP hozzáadása (admin)")]
    public async Task AddPointsAsync(SocketUser user, int pointsToAdd)
    {
        await DeferAsync().ConfigureAwait(false);
        var points = await Database.AddPointsAsync(Context.Guild.Id, user.Id, pointsToAdd).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Pontok beállítva!",
            $"{user.Mention} mostantól {points.ToString()} XP-vel rendelkezik!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("addlevel", "Szint hozzáadása (admin)")]
    public async Task AddLevelAsync(SocketUser user, int levelsToAdd)
    {
        await DeferAsync().ConfigureAwait(false);
        var level = await Database.AddLevelAsync(Context.Guild.Id, user.Id, levelsToAdd).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!",
            $"{user.Mention} mostantól {level.ToString()} szintű!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("setlevel", "Szint állítása (admin)")]
    public async Task SetLevelAsync(SocketUser user, int level)
    {
        await DeferAsync().ConfigureAwait(false);
        var newLevel = await Database.SetLevelAsync(Context.Guild.Id, user.Id, level).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!",
            $"{user.Mention} mostantól {newLevel.ToString()} szintű!").ConfigureAwait(false);
    }
}