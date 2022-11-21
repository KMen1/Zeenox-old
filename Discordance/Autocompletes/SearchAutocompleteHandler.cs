using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;
using Discordance.Services;
using Lavalink4NET.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Autocompletes;

public sealed class SearchAutocompleteHandler : AutocompleteHandler
{
    private static readonly Regex SpotifyRegex = new(@"^(spotify:|https://[a-z]+\.spotify\.com/)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex YoutubeRegex =
        new(
            @"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (autocompleteInteraction.Data.Current.Value is not string query || query.Length < 3)
            return AutocompletionResult.FromSuccess();

        var audioService = services.GetRequiredService<AudioService>();
        var response = await audioService.SearchAsync(query);
        var tracks = response.Tracks;
        if (response.LoadType is TrackLoadType.LoadFailed or TrackLoadType.NoMatches || tracks is null ||
            tracks.Length == 0)
            return AutocompletionResult.FromSuccess();

        if (SpotifyRegex.IsMatch(query))
        {
            var uri = new Uri(query);
            if (uri.Segments.Length < 2)
                return AutocompletionResult.FromSuccess();

            if (response.LoadType != TrackLoadType.PlaylistLoaded)
                return AutocompletionResult.FromSuccess(tracks.Select(x =>
                    new AutocompleteResult(x.Title.TrimTo(99), $"st{x.TrackIdentifier}")));

            var isPlaylist = uri.Segments[1] == "playlist/";
            return AutocompletionResult.FromSuccess(
                new[]
                {
                    new AutocompleteResult(response.PlaylistInfo?.Name?.TrimTo(99),
                        isPlaylist ? "sp" : "sa" + uri.Segments.Last())
                });
        }

        if (response.LoadType == TrackLoadType.PlaylistLoaded)
            return AutocompletionResult.FromSuccess(
                new[]
                {
                    new AutocompleteResult(response.PlaylistInfo?.Name?.TrimTo(99),
                        $"yp{new Uri(query).Segments.Last()}")
                });

        return AutocompletionResult.FromSuccess(tracks.Select(x =>
            new AutocompleteResult(x.Title.TrimTo(99), $"yt{x.TrackIdentifier}")));
    }
}