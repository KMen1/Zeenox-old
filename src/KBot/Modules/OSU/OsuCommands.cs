using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using OsuSharp.Domain;
using OsuSharp.Interfaces;
using IUser = OsuSharp.Interfaces.IUser;

namespace KBot.Modules.OSU;

[Group("osu", "osu! parancsok")]
public class Osu : KBotModuleBase
{
    [SlashCommand("set", "osu! profil beállítása")]
    public async Task OsuSetProfile(string link)
    {
        if (!link.Contains("osu.ppy.sh/users") || !link.Contains("osu.ppy.sh/u"))
        {
            await RespondAsync("Kérlek adj meg egy valós osu! profil linket!", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync(true).ConfigureAwait(false);
        var osuId = Convert.ToUInt64(link.Split("/").Last());
        await Database.UpdateUserAsync(Context.Guild, Context.User, x => x.OsuId = osuId).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Red, "Sikeresen beállítottad az osu! profilod!",
            "https://osu.ppy.sh/u/" + osuId).ConfigureAwait(false);
    }

    [SlashCommand("recent", "Legutóbbi osu! score-od információi")]
    public async Task OsuRecent(SocketUser user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        var osuId = (await Database.GetUserAsync(Context.Guild, user ?? Context.User).ConfigureAwait(false)).OsuId;
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "Nincs osu! profil beállítva!",
                "Kérlek állítsd be osu! profilodat a `osu set` parancs segítségével!").ConfigureAwait(false);
            return;
        }

        var score = await OsuClient.GetUserScoresAsync((long)osuId, ScoreType.Recent, true, GameMode.Osu, 1).ConfigureAwait(false);
        if (score.Count == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "Az elmúlt 24 órában nincs osu! scoreod!",
                "Kérlek próbáld meg később!").ConfigureAwait(false);
            return;
        }

        await FollowUpWithScoreAsync(score[0], sw).ConfigureAwait(false);
    }

    [SlashCommand("stats", "osu! statisztikák")]
    public async Task OsuStats(SocketUser user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        var osuId = (await Database.GetUserAsync(Context.Guild, user ?? Context.User).ConfigureAwait(false)).OsuId;
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "Nincs osu! profil beállítva!",
                "Kérlek állítsd be osu! profilodat a `osu set` parancs segítségével!").ConfigureAwait(false);
            return;
        }

        var osuUser = await OsuClient.GetUserAsync((long)osuId, GameMode.Osu).ConfigureAwait(false);
        var playStyle = osuUser.Playstyle[0] == "mouse" ? "Egér, Billentyűzet" : "Rajztábla";
        var eb = new EmbedBuilder()
            .WithAuthor(osuUser.Username, osuUser.AvatarUrl.ToString(), $"https://osu.ppy.sh/users/{osuUser.Id}")
            .WithColor(Color.Gold)
            .AddField("📅 Regisztrált", $"`{osuUser.JoinDate.Humanize()}`", true)
            .AddField("🌍 Ország", $"`{osuUser.Country.Name}`", true)
            .AddField("🎚️ Szint", $"`{osuUser.Statistics.UserLevel.Current.ToString()}`", true)
            .AddField("🥇 Globál Rank",
                $"`# {osuUser.Statistics.GlobalRank.ToString("n0")} ({Math.Round(osuUser.Statistics.Pp).ToString(CultureInfo.CurrentCulture)}PP)`",
                true)
            .AddField("🥇 Országos Rank", $"`# {osuUser.Statistics.CountryRank.ToString("n0")}`", true)
            .AddField("🎯 Pontosság", $"`{Math.Round(osuUser.Statistics.HitAccuracy, 1).ToString()} %`", true)
            .AddField("🕐 Játékidő",
                $"`{TimeSpan.FromSeconds(osuUser.Statistics.PlayTime).Humanize()} ({osuUser.Statistics.PlayCount.ToString()} játék)`",
                true)
            .AddField("🎮 Max Combó", $"`{osuUser.Statistics.MaximumCombo.ToString()} x`", true)
            .AddField("🎹 Ezzel játszik", $"`{playStyle}`", true);
        sw.Stop();
        eb.WithDescription($"{sw.ElapsedMilliseconds} ms");
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }

    [SlashCommand("topserver", "Top 10 osu! játékos a szeveren")]
    public async Task OsuTop()
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        var users = await Database.GetOsuIdsAsync(Context.Guild.Id, 10).ConfigureAwait(false);
        var userOsuPair = new Dictionary<SocketUser, IUser>();
        foreach (var (userId, osuId) in users)
        {
            userOsuPair.Add(Context.Client.GetUser(userId), await OsuClient.GetUserAsync((long)osuId, GameMode.Osu).ConfigureAwait(false));
        }

        var userOsuPairList = userOsuPair.ToList();
        userOsuPairList.Sort((x, y) => x.Value.Statistics.GlobalRank.CompareTo(y.Value.Statistics.GlobalRank));
        var eb = new EmbedBuilder()
            .WithColor(Color.Gold)
            .WithTitle("Top 10 osu! játékos a szerveren");
        var desc = new StringBuilder();
        var i = 0;
        foreach (var (user, osuUser) in userOsuPairList)
        {
            i++;
            desc.AppendLine(
                $"{i}. {user.Mention} : [`# {osuUser.Statistics.GlobalRank:n0} ({Math.Round(osuUser.Statistics.Pp).ToString(CultureInfo.CurrentCulture)} PP)`](https://osu.ppy.sh/u/{osuUser.Id})");
        }
        eb.WithDescription(desc.ToString());
        sw.Stop();
        eb.WithFooter($"{sw.ElapsedMilliseconds} ms");
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }

    [SlashCommand("topplay", "Legjobb osu! played lekérése")]
    public async Task OsuTopPlay(SocketUser user = null)
    {
        await DeferAsync().ConfigureAwait(false);
        var sw = new Stopwatch();
        var osuId = (await Database.GetUserAsync(Context.Guild, user ?? Context.User).ConfigureAwait(false)).OsuId;
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(Color.Red, "Nincs osu! profil beállítva!",
                "Kérlek állítsd be osu! profilodat a `osu set` parancs segítségével!").ConfigureAwait(false);
            return;
        }

        var score = await OsuClient.GetUserScoresAsync((long)osuId, ScoreType.Best, true, GameMode.Osu, 1).ConfigureAwait(false);
        await FollowUpWithScoreAsync(score[0], sw).ConfigureAwait(false);
    }

    private async Task FollowUpWithScoreAsync(IScore score, Stopwatch sw)
    {
        var beatmap = await score.Client.GetBeatmapAsync(score.Beatmap.Id).ConfigureAwait(false);
        var pp = score.PerformancePoints is null ? 0 : Math.Round((double) score.PerformancePoints, 2);
        var mods = score.Mods.Count == 0 ? "No Mod" : string.Concat(score.Mods).ToUpper();
        sw.Stop();
        var eb = new EmbedBuilder()
            .WithAuthor(
                $"{score.Beatmapset.Title} [{score.Beatmap.Version}] +{mods} [{score.Beatmap.DifficultyRating.ToString(CultureInfo.InvariantCulture).Replace(",", ".")}★]",
                score.User.AvatarUrl.ToString(), score.Beatmap.Url)
            .WithThumbnailUrl($"https://b.ppy.sh/thumb/{score.Beatmapset.Id.ToString()}.jpg")
            .WithDescription(
                $"▸ {Enum.Parse<Grade>(score.Rank).GetGradeEmoji()}▸ {score.Accuracy:P2} ▸ **{pp.ToString(CultureInfo.InvariantCulture)}PP** \n " +
                $"▸ {score.TotalScore:n0} ▸ x{score.MaxCombo.ToString()}/{beatmap.MaxCombo.ToString()} ▸ [{score.Statistics.Count300.ToString()}/{score.Statistics.Count100.ToString()}/{score.Statistics.Count50.ToString()}/{score.Statistics.CountMiss.ToString()}]")
            .WithColor(Enum.Parse<Grade>(score.Rank).GetGradeColor())
            .WithFooter($"{score.User.Username} - {score.CreatedAt.Humanize(culture: new CultureInfo("hu-HU"))} - {sw.ElapsedMilliseconds} ms",
                "https://cdn.discordapp.com/emojis/864051085810991164.webp?size=96&quality=lossless")
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}