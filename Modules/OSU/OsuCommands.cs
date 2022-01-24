using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using KBot.Database;
using KBot.Enums;
using KBot.Helpers;

namespace KBot.Modules.OSU;

[Group("osu", "osu! parancsok")]
public class Osu : KBotModuleBase
{
    public DatabaseService Database { get; set; }

    [SlashCommand("set", "osu! profil beállítása")]
    public async Task OsuSetProfile(string link)
    {
        if (!link.Contains("osu.ppy.sh/users/") || !link.Contains("osu.ppy.sh/u/"))
        {
            await RespondWithEmbedAsync(EmbedResult.Error, "Hibás link!", "Kérlek adj meg egy valós osu! profil linket!").ConfigureAwait(false);
            return;
        }
        await DeferAsync().ConfigureAwait(false);
        var osuId = Convert.ToUInt64(link.Split("/").Last());
        await Database.SetUserOsuIdAsync(Context.Guild.Id, Context.User.Id, osuId).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Sikeresen beállítottad az osu! profilod!", "https://osu.ppy.sh/u/" + osuId).ConfigureAwait(false);
    }

    [SlashCommand("recent", "Legutóbbi osu! played információi")]
    public async Task OsuRecent()
    {
        await DeferAsync().ConfigureAwait(false);
        var osuId = await Database.GetUserOsuIdAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Nincs osu! profil beállítva!", "Kérlek állítsd be osu! profilodat a `osu set` parancs segítségével!").ConfigureAwait(false);
            return;
        }
        var score = await OsuService.GetRecentScoreAsync(osuId).ConfigureAwait(false);
        var beatmap = await OsuService.GetBeatMapByIdAsync(score.Beatmap.Id).ConfigureAwait(false);
        var pp = score.PP is null ? 0 : Math.Round((double)score.PP);
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
               IconUrl = score.User.AvatarUrl,
               Name = $"{score.Beatmapset.Title} [{score.Beatmap.Version}] [{score.Beatmap.DifficultyRating.ToString(CultureInfo.InvariantCulture).Replace(",", ".")}★]",
               Url = score.Beatmap.Url
            },
            ThumbnailUrl = $"https://b.ppy.sh/thumb/{score.Beatmapset.Id.ToString()}.jpg",
            Description = $"▸ :rankingA: ▸ {score.Accuracy:P2} ▸ **{pp.ToString(CultureInfo.InvariantCulture)}PP** \n " +
                          $"▸ {score.Score_:n0} ▸ x{score.MaxCombo.ToString()}/{beatmap.MaxCombo.ToString()} ▸ [{score.Statistics.Count300.ToString()}/{score.Statistics.Count100.ToString()}/{score.Statistics.Count50.ToString()}/{score.Statistics.CountMiss.ToString()}]",
            Color = OsuService.GetColorFromGrade(score.Grade),
            Footer = new EmbedFooterBuilder
            {
                Text = $"{score.User.Username} - {score.CreatedAt.Humanize(culture: new CultureInfo("hu-HU"))}"
            }
        }.Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}