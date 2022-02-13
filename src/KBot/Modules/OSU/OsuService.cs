using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using KBot.Common;
using Microsoft.Extensions.Hosting;
using osu.API.Client.Auth;
using osu.API.Data;
using osu.API.Data.Enums;
using osu.API.Requests.Beatmap;
using osu.API.Requests.Parameters;
using osu.API.Requests.QueryParams;
using osu.API.Requests.UrlParams;
using osu.API.Requests.Users;
using Serilog;
using User = osu.API.Data.User;

namespace KBot.Modules.OSU;

public class OsuService : BackgroundService
{
    private static ClientCredentialsGrant _credentials;

    public OsuService(BotConfig config)
    {
        _credentials = new ClientCredentialsGrant(config.OsuApi.AppId, config.OsuApi.AppSecret);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Logger.Information("osu! service Loaded");
        return Task.CompletedTask;
    }

    public static string GetEmojiFromGrade(ScoreGrade grade)
    {
        switch (grade)
        {
            case ScoreGrade.N:
                return "<:osuF:936588252763271168>";
            case ScoreGrade.F:
                return "<:osuF:936588252763271168>";
            case ScoreGrade.D:
                return "<:osuD:936588252884910130>";
            case ScoreGrade.C:
                return "<:osuC:936588253031723078>";
            case ScoreGrade.B:
                return "<:osuB:936588252830380042>";
            case ScoreGrade.A:
                return "<:osuA:936588252754882570>";
            case ScoreGrade.S:
                return "<:osuS:936588252872318996>";
            case ScoreGrade.SH:
                return "<:osuSH:936588252834574336>";
            case ScoreGrade.X:
                return "<:osuX:936588252402573333>";
            case ScoreGrade.XH:
                return "<:osuXH:936588252822007818>";
        }
        return "<:osuF:936588252763271168>";
    }

    public static async Task<User> GetUserAsync(ulong userId)
    {
        using var httpClient = new HttpClient();
        var token = await Authentication.OAuthClientCredentialsAsync(_credentials, httpClient).ConfigureAwait(false);

        var userRequest = new GetUserRequest(new UserUrlParam(userId), new ModeUrlParam(Gamemode.osu), token.Token);
        return await userRequest.GetAsync(httpClient).ConfigureAwait(false);
    }

    public static async Task<Score> GetScoreAsync(ulong userId, ScoreType scoreType)
    {
        using var httpClient = new HttpClient();
        var token = await Authentication.OAuthClientCredentialsAsync(_credentials, httpClient).ConfigureAwait(false);

        var scoresRequest = new GetUserScoresRequest(new UserUrlParam(userId), new ScoreTypeUrlParam(scoreType), token.Token, new IncludeFailsQueyParam(true), new ModeQueryParam(Gamemode.osu), new LimitQueryParam(1));
        var scores = await scoresRequest.GetAsync(httpClient).ConfigureAwait(false);
        return scores.FirstOrDefault();
    }

    public static async Task<Beatmap> GetBeatMapByIdAsync(ulong beatMapId)
    {
        using var httpClient = new HttpClient();
        var token = await Authentication.OAuthClientCredentialsAsync(_credentials, httpClient).ConfigureAwait(false);

        var beatmapRequest = new GetBeatmapRequest(new BeatmapUrlParam(beatMapId), token.Token);
        var beatmap = await beatmapRequest.GetAsync(httpClient).ConfigureAwait(false);

        return beatmap;
    }

    public static Color GetColorFromGrade(ScoreGrade scoreGrade)
    {
        switch (scoreGrade)
        {
            case ScoreGrade.N:
                return Color.Default;
            case ScoreGrade.F:
                return new Color(109, 73, 38);
            case ScoreGrade.D:
                return Color.Red;
            case ScoreGrade.C:
                return Color.Purple;
            case ScoreGrade.B:
                return Color.Blue;
            case ScoreGrade.A:
                return Color.Green;
            case ScoreGrade.S:
                return Color.Gold;
            case ScoreGrade.SH:
                return Color.LightGrey;
            case ScoreGrade.X:
                return Color.Gold;
            case ScoreGrade.XH:
                return Color.LightGrey;
        }

        return Color.Default;
    }
}