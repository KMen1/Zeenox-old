using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Services;
using Humanizer;
using OsuSharp.Domain;
using OsuSharp.Interfaces;

namespace Discordance.Modules.Osu;

public class Commands : ModuleBase
{
    private readonly IOsuClient _osuClient;
    private readonly ImageService _imageService;

    public Commands(IOsuClient osuClient, ImageService imageService)
    {
        _osuClient = osuClient;
        _imageService = imageService;
    }

    [SlashCommand("osu-link", "Link your osu! profile")]
    public async Task SetOsuProfileAsync(string username)
    {
        await DeferAsync(true).ConfigureAwait(false);

        var user = await _osuClient.GetUserAsync(username, GameMode.Osu).ConfigureAwait(false);
        await DatabaseService
            .UpdateUserAsync(Context.User.Id, x => x.OsuId = (ulong)user.Id)
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("**Your osu! profile has been linked!**")
                    .Build()
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("osu-score", "Gets a score from the specified user")]
    public async Task SendScoreAsync(ScoreType scoreType, string? username = null)
    {
        await DeferAsync().ConfigureAwait(false);
        IReadOnlyList<IScore> scores;

        if (username is null)
        {
            var user = await DatabaseService.GetUserAsync(Context.User.Id).ConfigureAwait(false);
            if (user.OsuId is null)
            {
                await FollowupAsync(
                        embed: new EmbedBuilder()
                            .WithColor(Color.Red)
                            .WithDescription("**You haven't linked your osu! profile yet!**")
                            .Build()
                    )
                    .ConfigureAwait(false);
                return;
            }
            scores = await _osuClient
                .GetUserScoresAsync((long)user.OsuId, scoreType, true, GameMode.Osu, 1)
                .ConfigureAwait(false);
        }
        else
        {
            var user = await _osuClient.GetUserAsync(username, GameMode.Osu).ConfigureAwait(false);
            scores = await _osuClient
                .GetUserScoresAsync(user.Id, scoreType, true, GameMode.Osu, 1)
                .ConfigureAwait(false);
        }

        if (scores.Count == 0)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("**No scores found!**")
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        var score = scores[0];
        var beatmap = await score.Client.GetBeatmapAsync(score.Beatmap.Id).ConfigureAwait(false);
        var pp = score.PerformancePoints is null ? 0 : Math.Round((double)score.PerformancePoints);
        var mods =
            score.Mods.Count == 0
                ? "No Mod"
                : string.Concat(score.Mods).ToUpper(CultureInfo.InvariantCulture);
        var grade = Enum.Parse<Grade>(score.Rank);
        var id = Guid.NewGuid();
        var eb = new EmbedBuilder()
            .WithAuthor(
                $"{score.Beatmapset.Title} [{score.Beatmap.Version}] + {mods} [{score.Beatmap.DifficultyRating.ToString(CultureInfo.InvariantCulture).Replace(",", ".", StringComparison.OrdinalIgnoreCase)}★]",
                score.User.AvatarUrl.ToString(),
                score.Beatmap.Url
            )
            .WithImageUrl($"attachment://{id}.png")
            .WithColor(grade.GetGradeColor())
            .WithFooter(
                $"{score.User.Username}",
                "https://cdn.discordapp.com/emojis/864051085810991164.webp?size=96&quality=lossless"
            )
            .AddField("Played", $"<t:{score.CreatedAt.ToUnixTimeSeconds()}:R>", true)
            .AddField("PP", $"`{pp}pp`", true)
            .AddField("Max combo on map", $"`{beatmap.MaxCombo.ToString()}x`", true)
            .Build();
        await FollowupWithFileAsync(
                _imageService.CreateScoreImage(score),
                $"{id}.png",
                ephemeral: true,
                embed: eb
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("osu-profile", "Check someone's osu! profile")]
    public async Task SendOsuStatsAsync(string? username = null)
    {
        await DeferAsync().ConfigureAwait(false);
        IGlobalUser? profile;
        if (username is null)
        {
            var user = await DatabaseService.GetUserAsync(Context.User.Id).ConfigureAwait(false);
            if (user.OsuId == 0)
            {
                await FollowupAsync(
                        embed: new EmbedBuilder()
                            .WithColor(Color.Red)
                            .WithDescription("**You haven't linked your osu! profile yet!**")
                            .Build()
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

        var playStyle = profile.Playstyle is null
            ? "Unavailable"
            : profile.Playstyle[0] == "mouse"
                ? "Mouse"
                : "Tablet";
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
            .AddField("🎹 Plays with", $"`{playStyle}`", true);
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }
}
