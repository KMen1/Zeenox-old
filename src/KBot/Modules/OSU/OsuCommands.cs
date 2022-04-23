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

[Group("osu", "osu! parancsok")]
public class OsuCommands : SlashModuleBase
{
    private readonly OsuClient _osuClient;
    public OsuCommands(OsuClient osuClient)
    {
        _osuClient = osuClient;
    }

    [SlashCommand("set", "Link your osu! profile")]
    public async Task SetOsuProfileAsync(string link)
    {
        if (!link.Contains("osu.ppy.sh/users", StringComparison.OrdinalIgnoreCase) || !link.Contains("osu.ppy.sh/u", StringComparison.OrdinalIgnoreCase))
        {
            await RespondAsync("That's not a osu! profile link!", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync(true).ConfigureAwait(false);
        var result = ulong.TryParse(link.Split("/").Last(), out var osuId);
        await Mongo.UpdateUserAsync((SocketGuildUser)Context.User, x => x.OsuId = osuId).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Red, "Succesfully linked your osu! profile!",
            "https://osu.ppy.sh/u/" + osuId).ConfigureAwait(false);
    }

    [SlashCommand("recent", "Gets the recent play of a user")]
    public async Task SendRecentOsuPlayAsync(SocketUser? user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        var osuId = (await Mongo.GetUserAsync((SocketGuildUser)(user ?? Context.User)).ConfigureAwait(false)).OsuId;
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "No osu! profile linked!",
                "Please link your osu! profile using **/osu set**!").ConfigureAwait(false);
            return;
        }

        var score = await _osuClient.GetUserScoresAsync((long) osuId, ScoreType.Recent, true, GameMode.Osu, 1)
            .ConfigureAwait(false);
        if (score.Count == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "No recent plays in the last 24 hours!",
                "Come back after playing!").ConfigureAwait(false);
            return;
        }

        await FollowUpWithScoreAsync(score[0], sw).ConfigureAwait(false);
    }

    [SlashCommand("stats", "osu! statistics")]
    public async Task SendOsuStatsAsync(SocketUser? user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        var osuId = (await Mongo.GetUserAsync((SocketGuildUser)(user ?? Context.User)).ConfigureAwait(false)).OsuId;
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "No osu! profile linked!",
                "Please link your osu! profile using **/osu set**").ConfigureAwait(false);
            return;
        }

        var osuUser = await _osuClient.GetUserAsync((long) osuId, GameMode.Osu).ConfigureAwait(false);
        var playStyle = string.Equals(osuUser.Playstyle[0], "mouse", StringComparison.OrdinalIgnoreCase) ? "Mouse" : "Tablet";
        var eb = new EmbedBuilder()
            .WithAuthor(osuUser.Username, osuUser.AvatarUrl.ToString(), $"https://osu.ppy.sh/users/{osuUser.Id}")
            .WithColor(Color.Gold)
            .AddField("📅 Registered", $"`{osuUser.JoinDate.Humanize()}`", true)
            .AddField("🌍 Country", $"`{osuUser.Country.Name}`", true)
            .AddField("🎚️ Level", $"`{osuUser.Statistics.UserLevel.Current.ToString(CultureInfo.InvariantCulture)}`", true)
            .AddField("🥇 Global Rank",
                $"`# {osuUser.Statistics.GlobalRank.ToString("n0", CultureInfo.InvariantCulture)} ({Math.Round(osuUser.Statistics.Pp).ToString(CultureInfo.CurrentCulture)}PP)`",
                true)
            .AddField("🥇 Country Rank", $"`# {osuUser.Statistics.CountryRank.ToString("n0", CultureInfo.InvariantCulture)}`", true)
            .AddField("🎯 Accuracy", $"`{Math.Round(osuUser.Statistics.HitAccuracy, 1).ToString(CultureInfo.InvariantCulture)} %`", true)
            .AddField("🕐 Playtime",
                $"`{TimeSpan.FromSeconds(osuUser.Statistics.PlayTime).Humanize()} ({osuUser.Statistics.PlayCount.ToString(CultureInfo.InvariantCulture)} játék)`",
                true)
            .AddField("🎮 Max Combo", $"`{osuUser.Statistics.MaximumCombo.ToString(CultureInfo.InvariantCulture)} x`", true)
            .AddField("🎹 Plays with", $"`{playStyle}`", true);
        sw.Stop();
        eb.WithDescription($"{sw.ElapsedMilliseconds} ms");
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }

    [SlashCommand("topserver", "Sends the top 10 osu! players in the server")]
    public async Task SendTopOsuPlayersAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        var users = await Mongo.GetOsuIdsAsync(Context.Guild, 10).ConfigureAwait(false);
        var userOsuPair = new Dictionary<SocketUser, IUser>();
        foreach (var (userId, osuId) in users)
            userOsuPair.Add(Context.Client.GetUser(userId),
                await _osuClient.GetUserAsync((long) osuId, GameMode.Osu).ConfigureAwait(false));

        var userOsuPairList = userOsuPair.ToList();
        userOsuPairList.Sort((x, y) => x.Value.Statistics.GlobalRank.CompareTo(y.Value.Statistics.GlobalRank));
        var eb = new EmbedBuilder()
            .WithColor(Color.Gold)
            .WithTitle("Top 10 osu! players in the server");
        var desc = "";
        var i = 0;
        foreach (var (user, osuUser) in userOsuPairList)
        {
            i++;
            desc += $"{i}. {user.Mention} : [`# {osuUser.Statistics.GlobalRank:n0} ({Math.Round(osuUser.Statistics.Pp).ToString(CultureInfo.CurrentCulture)} PP)`](https://osu.ppy.sh/u/{osuUser.Id})\n";
        }

        eb.WithDescription(desc);
        sw.Stop();
        eb.WithFooter($"{sw.ElapsedMilliseconds} ms");
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }

    [SlashCommand("topplay", "Sends the top play of a user")]
    public async Task SendOsuTopPlayAsync(SocketUser? user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = new Stopwatch();
        var osuId = (await Mongo.GetUserAsync((SocketGuildUser)(user ?? Context.User)).ConfigureAwait(false)).OsuId;
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "No osu! profile linked",
                "Please link your osu! profile using **/osu set**").ConfigureAwait(false);
            return;
        }

        var score = await _osuClient.GetUserScoresAsync((long) osuId, ScoreType.Best, true, GameMode.Osu, 1)
            .ConfigureAwait(false);
        await FollowUpWithScoreAsync(score[0], sw).ConfigureAwait(false);
    }

    private async Task FollowUpWithScoreAsync(IScore score, Stopwatch sw)
    {
        var beatmap = await score.Client.GetBeatmapAsync(score.Beatmap.Id).ConfigureAwait(false);
        var pp = score.PerformancePoints is null ? 0 : Math.Round((double) score.PerformancePoints, 2);
        var mods = score.Mods.Count == 0 ? "No Mod" : string.Concat(score.Mods).ToUpper(CultureInfo.InvariantCulture);
        sw.Stop();
        var eb = new EmbedBuilder()
            .WithAuthor(
                $"{score.Beatmapset.Title} [{score.Beatmap.Version}] +{mods} [{score.Beatmap.DifficultyRating.ToString(CultureInfo.InvariantCulture).Replace(",", ".", StringComparison.OrdinalIgnoreCase)}★]",
                score.User.AvatarUrl.ToString(), score.Beatmap.Url)
            .WithThumbnailUrl($"https://b.ppy.sh/thumb/{score.Beatmapset.Id.ToString(CultureInfo.InvariantCulture)}.jpg")
            .WithDescription(
                $"▸ {Enum.Parse<Grade>(score.Rank).GetGradeEmoji()}▸ {score.Accuracy:P2} ▸ **{pp.ToString(CultureInfo.InvariantCulture)}PP** \n " +
                $"▸ {score.TotalScore:n0} ▸ x{score.MaxCombo.ToString(CultureInfo.InvariantCulture)}/{beatmap.MaxCombo.ToString()} ▸ [{score.Statistics.Count300.ToString(CultureInfo.InvariantCulture)}/{score.Statistics.Count100.ToString(CultureInfo.InvariantCulture)}/{score.Statistics.Count50.ToString(CultureInfo.InvariantCulture)}/{score.Statistics.CountMiss.ToString(CultureInfo.InvariantCulture)}]")
            .WithColor(Enum.Parse<Grade>(score.Rank).GetGradeColor())
            .WithFooter(
                $"{score.User.Username} - {score.CreatedAt.Humanize(culture: new CultureInfo("hu-HU"))} - {sw.ElapsedMilliseconds} ms",
                "https://cdn.discordapp.com/emojis/864051085810991164.webp?size=96&quality=lossless")
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}