using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using KBot.Config;
using osu.API.Client.Auth;
using osu.API.Data;
using osu.API.Data.Enums;
using osu.API.Requests.Beatmap;
using osu.API.Requests.Parameters;
using osu.API.Requests.QueryParams;
using osu.API.Requests.UrlParams;
using osu.API.Requests.Users;

namespace KBot.Modules.OSU;

public class OsuService
{
    private static ulong _id;
    private static string _appSecret;
    private static ConfigModel.Config _config;

    public OsuService(ConfigModel.Config config)
    {
        _config = config;
    }

    public void Initialize()
    {
        _id = _config.OsuApi.AppId;
        _appSecret = _config.OsuApi.AppSecret;
    }

    public static async Task<Score> GetRecentScoreAsync(ulong userId)
    {
        using var httpClient = new HttpClient();
        var credentials = new ClientCredentialsGrant(_id, _appSecret);
        var token = await Authentication.OAuthClientCredentialsAsync(credentials, httpClient).ConfigureAwait(false);

        var scoresRequest = new GetUserScoresRequest(new UserUrlParam(userId), new ScoreTypeUrlParam(ScoreType.RECENT), token.Token, new IncludeFailsQueyParam(true), new ModeQueryParam(Gamemode.osu), new LimitQueryParam(1));
        var scores = await scoresRequest.GetAsync(httpClient).ConfigureAwait(false);
        return scores.FirstOrDefault();
    }

    public static async Task<Beatmap> GetBeatMapByIdAsync(ulong beatMapId)
    {
        using var httpClient = new HttpClient();
        var credentials = new ClientCredentialsGrant(_id, _appSecret);
        var token = await Authentication.OAuthClientCredentialsAsync(credentials, httpClient).ConfigureAwait(false);

        var beatmapRequest = new GetBeatmapRequest(new BeatmapUrlParam(beatMapId), token.Token);
        var beatmap = await beatmapRequest.GetAsync(httpClient).ConfigureAwait(false);

        return beatmap;
    }

    public static Color? GetColorFromGrade(ScoreGrade scoreGrade)
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