using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Enums;
using osu.API.Data.Enums;
using User = osu.API.Data.User;

namespace KBot.Modules.OSU;

[Group("osu", "osu! parancsok")]
public class Osu : KBotModuleBase
{
    [SlashCommand("set", "osu! profil beállítása")]
    public async Task OsuSetProfile(string link)
    {
        if (!link.Contains("osu.ppy.sh/users") || !link.Contains("osu.ppy.sh/u"))
        {
            await RespondWithEmbedAsync(EmbedResult.Error, "Hibás link!", "Kérlek adj meg egy valós osu! profil linket!").ConfigureAwait(false);
            return;
        }
        await DeferAsync().ConfigureAwait(false);
        var osuId = Convert.ToUInt64(link.Split("/").Last());
        await Database.SetUserOsuIdAsync(Context.Guild.Id, Context.User.Id, osuId).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Sikeresen beállítottad az osu! profilod!", "https://osu.ppy.sh/u/" + osuId).ConfigureAwait(false);
    }

    [SlashCommand("recent", "Legutóbbi osu! score-od információi")]
    public async Task OsuRecent()
    {
        await DeferAsync().ConfigureAwait(false);
        var osuId = await Database.GetUserOsuIdAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Nincs osu! profil beállítva!", "Kérlek állítsd be osu! profilodat a `osu set` parancs segítségével!").ConfigureAwait(false);
            return;
        }
        var score = await OsuService.GetScoreAsync(osuId, ScoreType.RECENT).ConfigureAwait(false);
        if (score is null)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Az elmúlt 24 órában nincs osu! scoreod!", "Kérlek próbáld meg később!").ConfigureAwait(false);
            return;
        }
        var beatmap = await OsuService.GetBeatMapByIdAsync(score.Beatmap.Id).ConfigureAwait(false);
        var pp = score.PP is null ? 0 : Math.Round((double)score.PP, 2);
        var mods = score.Mods.Length == 0 ? "No Mod" : string.Concat(score.Mods).ToUpper();
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
               IconUrl = score.User.AvatarUrl,
               Name = $"{score.Beatmapset.Title} [{score.Beatmap.Version}] +{mods} [{score.Beatmap.DifficultyRating.ToString(CultureInfo.InvariantCulture).Replace(",", ".")}★]",
               Url = score.Beatmap.Url
            },
            ThumbnailUrl = $"https://b.ppy.sh/thumb/{score.Beatmapset.Id.ToString()}.jpg",
            Description = $"▸ {OsuService.GetEmojiFromGrade(score.Grade)}▸ {score.Accuracy:P2} ▸ **{pp.ToString(CultureInfo.InvariantCulture)}PP** \n " +
                          $"▸ {score.Score_:n0} ▸ x{score.MaxCombo.ToString()}/{beatmap.MaxCombo.ToString()} ▸ [{score.Statistics.Count300.ToString()}/{score.Statistics.Count100.ToString()}/{score.Statistics.Count50.ToString()}/{score.Statistics.CountMiss.ToString()}]",
            Color = OsuService.GetColorFromGrade(score.Grade),
            Footer = new EmbedFooterBuilder
            {
                Text = $"{score.User.Username} - {score.CreatedAt.Humanize(culture: new CultureInfo("hu-HU"))}",
                IconUrl = "https://cdn.discordapp.com/emojis/864051085810991164.webp?size=96&quality=lossless"
            }
        }.Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
    [SlashCommand("stats", "osu! statisztikák")]
    public async Task OsuStats()
    {
        await DeferAsync().ConfigureAwait(false);
        var osuId = await Database.GetUserOsuIdAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Nincs osu! profil beállítva!", "Kérlek állítsd be osu! profilodat a `osu set` parancs segítségével!").ConfigureAwait(false);
            return;
        }
        var user = await OsuService.GetUserAsync(osuId).ConfigureAwait(false);
        var playStyle = "";
        if (user.PlayStyle[0] == "mouse")
        {
            playStyle = "Egér, Billentyűzet";
        }
        else
        {
            playStyle = "Rajztábla";
        }
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                IconUrl = user.AvatarUrl,
                Name = user.Username,
                Url = $"https://osu.ppy.sh/users/{user.Id}"
            },
            Color = Color.Gold,
            Fields =
            {
                new()
                {
                    Name = "📅 Regisztrált",
                    Value = $"`{user.JoinDate.Humanize()}`",
                    IsInline = true
                },
                new()
                {
                    Name = "🌍 Ország",
                    Value = $"`{user.Country.Name}`",
                    IsInline = true
                },
                new()
                {
                    Name = "🎚️ Szint",
                    Value = $"`{user.Statistics.Level.Current.ToString()}`",
                    IsInline = true
                },
                new()
                {
                    Name = "🥇 Globál Rank",
                    Value = $"`# {user.Statistics.GlobalRank:n0} ({Math.Round(user.Statistics.PP).ToString(CultureInfo.CurrentCulture)}PP)`",
                    IsInline = true
                },
                new()
                {
                    Name = "🥇 Országos Rank",
                    Value = $"`# {user.Statistics.CountryRank:n0}`",
                    IsInline = true
                },
                new()
                {
                    Name = "🎯 Pontosság",
                    Value = $"`{Math.Round(user.Statistics.HitAccuracy, 1)} %`",
                    IsInline = true
                },
                new()
                {
                    Name = "🕐 Játékidő",
                    Value = $"`{TimeSpan.FromSeconds(user.Statistics.PlayTime).Humanize()} ({user.Statistics.PlayCount.ToString()} játék)`",
                    IsInline = true
                },
                new()
                {
                    Name = "🎮 Max Combó",
                    Value = $"`{user.Statistics.MaximumCombo.ToString()} x`",
                    IsInline = true
                },
                new()
                {
                    Name = "🎹 Ezzel játszik",
                    Value = $"`{playStyle}`",
                    IsInline = true
                }
            }
        }.Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    [SlashCommand("topserver", "Top 10 osu! játékos a szeveren")]
    public async Task OsuTop()
    {
        await DeferAsync().ConfigureAwait(false);
        var users = await Database.GetOsuIdsAsync(Context.Guild.Id, 10).ConfigureAwait(false);
        var userOsuPair = new Dictionary<SocketUser, User>();
        foreach (var (userId, osuId) in users)
        {
            userOsuPair.Add(Context.Client.GetUser(userId), await OsuService.GetUserAsync(osuId).ConfigureAwait(false));
        }
        var userOsuPairList = userOsuPair.ToList();
        userOsuPairList.Sort((x, y) => x.Value.Statistics.GlobalRank.CompareTo(y.Value.Statistics.GlobalRank));
        var eb = new EmbedBuilder
        {
            Color = Color.Gold,
            Title = "Top 10 osu! játékos a szerveren",
        };
        var desc = new StringBuilder();
        var i = 0;
        foreach (var (user, osuUser) in userOsuPairList)
        {
            i++;
            desc.AppendLine($"{i}. {user.Mention} : [`# {osuUser.Statistics.GlobalRank:n0} ({Math.Round(osuUser.Statistics.PP).ToString(CultureInfo.CurrentCulture)} PP)`](https://osu.ppy.sh/u/{osuUser.Id})");
        }
        eb.Description = desc.ToString();
        await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
    }

    [SlashCommand("topplay", "Legjobb osu! played lekérése")]
    public async Task OsuTopPlay()
    {
        await DeferAsync().ConfigureAwait(false);
        var osuId = await Database.GetUserOsuIdAsync(Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
        if (osuId == 0)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Nincs osu! profil beállítva!", "Kérlek állítsd be osu! profilodat a `osu set` parancs segítségével!").ConfigureAwait(false);
            return;
        }
        var score = await OsuService.GetScoreAsync(osuId, ScoreType.BEST).ConfigureAwait(false);
        var beatmap = await OsuService.GetBeatMapByIdAsync(score.Beatmap.Id).ConfigureAwait(false);
        var pp = score.PP is null ? 0 : Math.Round((double)score.PP, 2);

        var mods = score.Mods.Length == 0 ? "No Mod" : string.Concat(score.Mods).ToUpper();
        var eb = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                IconUrl = score.User.AvatarUrl,
                Name = $"{score.Beatmapset.Title} [{score.Beatmap.Version}] +{mods} [{score.Beatmap.DifficultyRating.ToString(CultureInfo.InvariantCulture).Replace(",", ".")}★]",
                Url = score.Beatmap.Url
            },
            ThumbnailUrl = $"https://b.ppy.sh/thumb/{score.Beatmapset.Id.ToString()}.jpg",
            Description = $"▸ {OsuService.GetEmojiFromGrade(score.Grade)}▸ {score.Accuracy:P2} ▸ **{pp.ToString(CultureInfo.InvariantCulture)}PP** \n " +
                          $"▸ {score.Score_:n0} ▸ x{score.MaxCombo.ToString()}/{beatmap.MaxCombo.ToString()} ▸ [{score.Statistics.Count300.ToString()}/{score.Statistics.Count100.ToString()}/{score.Statistics.Count50.ToString()}/{score.Statistics.CountMiss.ToString()}]",
            Color = OsuService.GetColorFromGrade(score.Grade),
            Footer = new EmbedFooterBuilder
            {
                Text = $"{score.User.Username} - {score.CreatedAt.Humanize(culture: new CultureInfo("hu-HU"))}",
                IconUrl = "https://cdn.discordapp.com/emojis/864051085810991164.webp?size=96&quality=lossless"
            }
        }.Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}