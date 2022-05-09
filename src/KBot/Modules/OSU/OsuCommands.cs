using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using KBot.Extensions;
using OsuSharp;
using OsuSharp.Domain;
using OsuSharp.Interfaces;
using IUser = OsuSharp.Interfaces.IUser;

namespace KBot.Modules.OSU;

[DefaultMemberPermissions(GuildPermission.SendMessages)]
[Group("osu", "osu! commands")]
public class OsuCommands : SlashModuleBase
{
    private readonly OsuClient _osuClient;

    public OsuCommands(OsuClient osuClient)
    {
        _osuClient = osuClient;
    }

    [SlashCommand("link", "Link your osu! profile")]
    public async Task SetOsuProfileAsync(string username)
    {
        await DeferAsync(true).ConfigureAwait(false);

        var user = await _osuClient.GetUserAsync(username, GameMode.Osu).ConfigureAwait(false);
        await Mongo.UpdateUserAsync(GuildUser, x => x.OsuId = (ulong)user.Id).ConfigureAwait(false);
        await FollowupWithEmbedAsync(
                Color.Green,
                "Succesfully linked your osu! profile!",
                $"https://osu.ppy.sh/u/{user.Id}"
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("recent", "Gets the recent play of a user")]
    public async Task SendRecentOsuPlayAsync(string? username = null)
    {
        await DeferAsync().ConfigureAwait(false);
        IReadOnlyList<IScore> scores;
        if (username is null)
        {
            var user = await Mongo.GetUserAsync(GuildUser).ConfigureAwait(false);
            if (user.OsuId == 0)
            {
                await FollowupWithEmbedAsync(
                        Color.Red,
                        "You haven't linked your osu! profile yet!",
                        "Use `osu set <username>` to link your osu! profile"
                    )
                    .ConfigureAwait(false);
                return;
            }
            scores = await _osuClient
                .GetUserScoresAsync((long)user.OsuId, ScoreType.Recent, true, GameMode.Osu, 1)
                .ConfigureAwait(false);
        }
        else
        {
            var user = await _osuClient.GetUserAsync(username, GameMode.Osu).ConfigureAwait(false);
            scores = await _osuClient
                .GetUserScoresAsync(user.Id, ScoreType.Recent, true, GameMode.Osu, 1)
                .ConfigureAwait(false);
        }

        if (scores.Count == 0)
        {
            await FollowupWithEmbedAsync(
                    Color.Red,
                    "User has not played in the last 24 hours!",
                    "Try again later!"
                )
                .ConfigureAwait(false);
            return;
        }

        await FollowUpWithScoreAsync(scores[0]).ConfigureAwait(false);
    }

    [SlashCommand("profile", "Check someone's osu! profile")]
    public async Task SendOsuStatsAsync(string? username = null)
    {
        await DeferAsync().ConfigureAwait(false);
        IGlobalUser? profile;
        if (username is null)
        {
            var user = await Mongo.GetUserAsync(GuildUser).ConfigureAwait(false);
            if (user.OsuId == 0)
            {
                await FollowupWithEmbedAsync(
                        Color.Red,
                        "You haven't linked your osu! profile yet!",
                        "Use `osu set <username>` to link your osu! profile"
                    )
                    .ConfigureAwait(false);
                return;
            }
            profile = await _osuClient
                .GetUserAsync((long)user.OsuId, GameMode.Osu)
                .ConfigureAwait(false);
        }
        else
        {
            profile = await _osuClient.GetUserAsync(username, GameMode.Osu).ConfigureAwait(false);
        }
        /*var playStyle = string.Equals(
            profile.Playstyle[0],
            "mouse",
            StringComparison.OrdinalIgnoreCase
        )
          ? "Mouse + Keyboard"
          : "Tablet + Keyboard";*/
        var eb = new EmbedBuilder()
            .WithAuthor(
                profile.Username,
                profile.AvatarUrl.ToString(),
                $"https://osu.ppy.sh/users/{profile.Id}"
            )
            .WithColor(Color.Gold)
            .AddField("📅 Registered", $"<t:{profile.JoinDate.ToUnixTimeSeconds()}:R>", true)
            .AddField("🌍 Country", $"`{profile.Country.Name}`", true)
            .AddField(
                "🎚️ Level",
                $"`{profile.Statistics.UserLevel.Current.ToString(CultureInfo.InvariantCulture)}`",
                true
            )
            .AddField(
                "🥇 Global Rank",
                $"`# {profile.Statistics.GlobalRank.ToString("n0", CultureInfo.InvariantCulture)} ({Math.Round(profile.Statistics.Pp).ToString(CultureInfo.CurrentCulture)}PP)`",
                true
            )
            .AddField(
                "🥇 Country Rank",
                $"`# {profile.Statistics.CountryRank.ToString("n0", CultureInfo.InvariantCulture)}`",
                true
            )
            .AddField(
                "🎯 Accuracy",
                $"`{Math.Round(profile.Statistics.HitAccuracy, 1).ToString(CultureInfo.InvariantCulture)} %`",
                true
            )
            .AddField(
                "🕐 Playtime",
                $"`{TimeSpan.FromSeconds(profile.Statistics.PlayTime).Humanize()} ({profile.Statistics.PlayCount.ToString(CultureInfo.InvariantCulture)} plays)`",
                true
            )
            .AddField(
                "🎮 Max Combo",
                $"`{profile.Statistics.MaximumCombo.ToString(CultureInfo.InvariantCulture)} x`",
                true
            )
            .AddField("🎹 Plays with", $"`Unknown`", true);
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }

    [SlashCommand("top", "Sends the top play of a user")]
    public async Task SendOsuTopPlayAsync(string? username = null)
    {
        await DeferAsync().ConfigureAwait(false);
        IReadOnlyList<IScore> scores;
        if (username is null)
        {
            var user = await Mongo.GetUserAsync(GuildUser).ConfigureAwait(false);
            if (user.OsuId == 0)
            {
                await FollowupWithEmbedAsync(
                        Color.Red,
                        "You haven't linked your osu! profile yet!",
                        "Use `osu set <username>` to link your osu! profile"
                    )
                    .ConfigureAwait(false);
                return;
            }
            scores = await _osuClient
                .GetUserScoresAsync((long)user.OsuId, ScoreType.Best, true, GameMode.Osu, 1)
                .ConfigureAwait(false);
        }
        else
        {
            var user = await _osuClient.GetUserAsync(username, GameMode.Osu).ConfigureAwait(false);
            scores = await _osuClient
                .GetUserScoresAsync(user.Id, ScoreType.Best, true, GameMode.Osu, 1)
                .ConfigureAwait(false);
        }

        if (scores.Count == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "User has no plays!", "Try again later!")
                .ConfigureAwait(false);
            return;
        }
        await FollowUpWithScoreAsync(scores[0]).ConfigureAwait(false);
    }

    private async Task FollowUpWithScoreAsync(IScore score)
    {
        var beatmap = await score.Client.GetBeatmapAsync(score.Beatmap.Id).ConfigureAwait(false);
        var pp = score.PerformancePoints is null
            ? 0
            : Math.Round((double)score.PerformancePoints, 2);
        var mods =
            score.Mods.Count == 0
                ? "No Mod"
                : string.Concat(score.Mods).ToUpper(CultureInfo.InvariantCulture);
        var eb = new EmbedBuilder()
            .WithAuthor(
                $"{score.Beatmapset.Title} [{score.Beatmap.Version}] +{mods} [{score.Beatmap.DifficultyRating.ToString(CultureInfo.InvariantCulture).Replace(",", ".", StringComparison.OrdinalIgnoreCase)}★]",
                score.User.AvatarUrl.ToString(),
                score.Beatmap.Url
            )
            .WithThumbnailUrl(
                $"https://b.ppy.sh/thumb/{score.Beatmapset.Id.ToString(CultureInfo.InvariantCulture)}.jpg"
            )
            .WithDescription(
                $"▸ {Enum.Parse<Grade>(score.Rank).GetGradeEmoji()}▸ {score.Accuracy:P2} ▸ **{pp.ToString(CultureInfo.InvariantCulture)}PP** \n "
                    + $"▸ {score.TotalScore:n0} ▸ x{score.MaxCombo.ToString(CultureInfo.InvariantCulture)}/{beatmap.MaxCombo.ToString()} ▸ [{score.Statistics.Count300.ToString(CultureInfo.InvariantCulture)}/{score.Statistics.Count100.ToString(CultureInfo.InvariantCulture)}/{score.Statistics.Count50.ToString(CultureInfo.InvariantCulture)}/{score.Statistics.CountMiss.ToString(CultureInfo.InvariantCulture)}]"
            )
            .WithColor(Enum.Parse<Grade>(score.Rank).GetGradeColor())
            .WithFooter(
                $"{score.User.Username} - {score.CreatedAt.Humanize(culture: new CultureInfo("hu-HU"))}",
                "https://cdn.discordapp.com/emojis/864051085810991164.webp?size=96&quality=lossless"
            )
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}
