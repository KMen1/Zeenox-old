using System.Text.RegularExpressions;
using Zeenox.Enums;

namespace Zeenox.Services;

public static partial class AudioHelper
{
    public static string GetUrlType(string url)
    {
        if (YoutubeRegex().IsMatch(url)) return "youtube";

        return SpotifyRegex().IsMatch(url) ? "spotify" : "unknown";
    }

    public static (string, SearchResultType) GetIdFromYoutubeUrl(string url)
    {
        var playlistMatch = YoutubePlaylistRegex().Match(url);
        if (playlistMatch.Success) return (playlistMatch.Groups[1].Value, SearchResultType.Playlist);

        var videoMatch = YoutubeVideoRegex().Match(url);
        return videoMatch.Success
            ? (videoMatch.Groups[1].Value, SearchResultType.Track)
            : (string.Empty, SearchResultType.Unknown);
    }

    public static (string, SearchResultType) GetIdFromSpotifyUrl(string url)
    {
        var playlistMatch = SpotifyPlaylistRegex().Match(url);
        if (playlistMatch.Success) return (playlistMatch.Groups[3].Value, SearchResultType.Playlist);

        var albumMatch = SpotifyAlbumRegex().Match(url);
        if (albumMatch.Success) return (albumMatch.Groups[3].Value, SearchResultType.Album);

        var trackMatch = SpotifyTrackRegex().Match(url);
        return trackMatch.Success
            ? (trackMatch.Groups[3].Value, SearchResultType.Track)
            : (string.Empty, SearchResultType.Unknown);
    }

    public static string ApplySearchMode(string query, SearchMode mode)
    {
        return mode switch
        {
            SearchMode.YouTube => $"ytsearch:{query}",
            SearchMode.Spotify => $"spsearch:{query}",
            _ => query
        };
    }

    [GeneratedRegex("^(https?:\\/\\/)?(www\\.)?(m\\.)?youtube\\.com\\/.+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "hu-HU")]
    private static partial Regex YoutubeRegex();

    [GeneratedRegex("^(https?:\\/\\/)?(open\\.)?spotify\\.com\\/.+$", RegexOptions.IgnoreCase | RegexOptions.Compiled,
        "hu-HU")]
    private static partial Regex SpotifyRegex();

    [GeneratedRegex("^.*(?:(?:playlist|p)\\?|&amp;|&)list=([^&]+).*$", RegexOptions.IgnoreCase | RegexOptions.Compiled,
        "hu-HU")]
    private static partial Regex YoutubePlaylistRegex();

    [GeneratedRegex(
        "^.*(?:(?:youtu\\.be\\/|v\\/|vi\\/|u\\/\\w\\/|embed\\/)|(?:(?:watch)?\\?v(?:i)?=|\\&v(?:i)?=))([^#\\&\\?]*).*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "hu-HU")]
    private static partial Regex YoutubeVideoRegex();

    [GeneratedRegex("^(https?:\\/\\/)?(open\\.)?spotify\\.com\\/playlist\\/([a-zA-Z0-9]+)\\/?.*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "hu-HU")]
    private static partial Regex SpotifyPlaylistRegex();

    [GeneratedRegex("^(https?:\\/\\/)?(open\\.)?spotify\\.com\\/album\\/([a-zA-Z0-9]+)\\/?.*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "hu-HU")]
    private static partial Regex SpotifyAlbumRegex();

    [GeneratedRegex("^(https?:\\/\\/)?(open\\.)?spotify\\.com\\/track\\/([a-zA-Z0-9]+)\\/?.*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "hu-HU")]
    private static partial Regex SpotifyTrackRegex();
}