using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discordance.Enums;
using Discordance.Models;
using Google.Apis.YouTube.v3;
using Lavalink4NET;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using SpotifyAPI.Web;

namespace Discordance.Services;

public sealed class SearchService
{
    private readonly LavalinkNode _lavalinkNode;
    private readonly SpotifyClient _spotifyClient;
    private readonly YouTubeService _youTubeService;

    public SearchService(YouTubeService youTubeService, SpotifyClient spotifyClient, IAudioService lavalinkNode)
    {
        _youTubeService = youTubeService;
        _spotifyClient = spotifyClient;
        _lavalinkNode = (LavalinkNode) lavalinkNode;
    }

    public async Task<LavalinkTrack[]> SearchAsync(string query, IUser user, AudioService.SearchMode searchMode,
        int limit = 1)
    {
        query = AudioHelper.ApplySearchMode(query, searchMode);
        var results = await _lavalinkNode.LoadTracksAsync(query).ConfigureAwait(false);
        var tracks = results.Tracks;
        if (tracks is null || tracks.Length == 0)
            return Array.Empty<LavalinkTrack>();

        foreach (var track in tracks)
            track.Context = new TrackContext {Requester = user};

        tracks[0].Context = new TrackContext
            {Requester = user, CoverUrl = await GetCoverUrl(tracks[0]).ConfigureAwait(false)};
        return results.LoadType is TrackLoadType.PlaylistLoaded ? tracks : tracks.Take(limit).ToArray();
    }

    public async Task<(string Title, string Url, string CoverUrl)[]> SearchAsync(string query)
    {
        var type = AudioHelper.GetUrlType(query);
        switch (type)
        {
            case "youtube":
            {
                return await HandleYoutubeSearchAsync(query).ConfigureAwait(false);
            }
            case "spotify":
            {
                return await HandleSpotifySearchAsync(query).ConfigureAwait(false);
            }
            default:
            {
                var request = _youTubeService.Search.List("snippet");
                request.Q = query;
                request.MaxResults = 24;
                request.Type = "video";
                var response = await request.ExecuteAsync().ConfigureAwait(false);
                var tracks = response.Items.Select(x =>
                        (x.Snippet.Title, $"https://www.youtube.com/watch?v={x.Id.VideoId}",
                            x.Snippet.Thumbnails.High.Url))
                    .ToArray();
                return tracks;
            }
        }
    }

    private async Task<(string Title, string Url, string CoverUrl)[]> HandleSpotifySearchAsync(string url)
    {
        var (id, idType) = AudioHelper.GetIdFromSpotifyUrl(url);

        switch (idType)
        {
            case SearchResultType.Playlist:
            {
                var playlist = await _spotifyClient.Playlists.Get(id).ConfigureAwait(false);
                return new[]
                {
                    ($"{playlist.Name} by {playlist.Owner?.DisplayName}",
                        $"https://open.spotify.com/playlist/{playlist.Id}",
                        playlist.Images?.FirstOrDefault()?.Url ?? string.Empty)
                };
            }
            case SearchResultType.Album:
            {
                var album = await _spotifyClient.Albums.Get(id).ConfigureAwait(false);
                return new[]
                {
                    ($"{string.Join(", ", album.Artists.Select(x => x.Name))} - {album.Name}",
                        $"https://open.spotify.com/album/{album.Id}",
                        album.Images?.FirstOrDefault()?.Url ?? string.Empty)
                };
            }
            case SearchResultType.Track:
            {
                var track = await _spotifyClient.Tracks.Get(id).ConfigureAwait(false);
                return new[]
                {
                    ($"{string.Join(", ", track.Artists.Select(x => x.Name))} - {track.Name}",
                        $"https://open.spotify.com/track/{track.Id}",
                        track.Album.Images.FirstOrDefault()?.Url ?? string.Empty)
                };
            }
            default:
                return Array.Empty<(string, string, string)>();
        }
    }

    private async Task<(string Title, string Url, string CoverUrl)[]> HandleYoutubeSearchAsync(string url)
    {
        var (id, idType) = AudioHelper.GetIdFromYoutubeUrl(url);
        switch (idType)
        {
            case SearchResultType.Playlist:
            {
                var listRequest = _youTubeService.Playlists.List("snippet");
                listRequest.Id = id;
                var listResponse = await listRequest.ExecuteAsync().ConfigureAwait(false);
                var playlist = listResponse.Items[0];
                return new[]
                {
                    ($"{playlist.Snippet.Title} by {playlist.Snippet.ChannelTitle}",
                        $"https://www.youtube.com/playlist?list={playlist.Id}",
                        playlist.Snippet.Thumbnails.High.Url)
                };
            }
            case SearchResultType.Track:
            {
                var trackRequest = _youTubeService.Videos.List("snippet");
                trackRequest.Id = url;
                trackRequest.MaxResults = 1;
                var response = await trackRequest.ExecuteAsync().ConfigureAwait(false);
                var tracks = response.Items.Select(x =>
                        (x.Snippet.Title, $"https://www.youtube.com/watch?v={x.Id}", x.Snippet.Thumbnails.High.Url))
                    .ToArray();
                return tracks;
            }
            default:
                return Array.Empty<(string, string, string)>();
        }
    }

    public async Task<string> GetCoverUrl(LavalinkTrack track)
    {
        switch (track.SourceName)
        {
            case "youtube":
            {
                var request = _youTubeService.Videos.List("snippet");
                request.Id = track.TrackIdentifier;
                request.MaxResults = 1;
                var response = await request.ExecuteAsync().ConfigureAwait(false);
                return response.Items[0].Snippet!.Thumbnails.High.Url;
            }
            case "spotify":
                var sTrack = await _spotifyClient.Tracks.Get(track.TrackIdentifier).ConfigureAwait(false);
                return sTrack.Album.Images[0].Url;
            default:
                return string.Empty;
        }
    }

    public async Task<LavalinkTrack[]> AddCoverUrls(LavalinkTrack[] track)
    {
        var spotifyTracks = track.Where(x => x.SourceName == "spotify").ToList();
        var spotifyIds = spotifyTracks.ConvertAll(x => x.TrackIdentifier);
        var youtubeTracks = track.Where(x => x.SourceName == "youtube").ToList();
        var youtubeIds = youtubeTracks.ConvertAll(x => x.TrackIdentifier);

        var spotifyResponse = new List<FullTrack>();

        for (var i = 0; i < (int) Math.Ceiling(spotifyIds.Count / (double) 50); i++)
        {
            var pack = spotifyIds.Skip(i * 50).Take(50).ToList();
            spotifyResponse.AddRange(
                (await _spotifyClient.Tracks.GetSeveral(new TracksRequest(pack)).ConfigureAwait(false)).Tracks);
        }

        if (youtubeIds.Count > 0)
        {
            var youtubeRequest = _youTubeService.Videos.List("snippet");
            youtubeRequest.Id = youtubeIds;
            var youtubeResponse = await youtubeRequest.ExecuteAsync().ConfigureAwait(false);
            youtubeTracks.ForEach(x => x.Context = (TrackContext) x.Context! with
            {
                CoverUrl = youtubeResponse.Items.First(y => y.Id == x.TrackIdentifier).Snippet!.Thumbnails.High.Url
            });
        }

        spotifyTracks.ForEach(x => x.Context = (TrackContext) x.Context! with
        {
            CoverUrl = spotifyResponse.First(y => y.Id == x.TrackIdentifier).Album.Images[0].Url
        });

        return new[] {spotifyTracks, youtubeTracks}.SelectMany(x => x).ToArray();
    }
}