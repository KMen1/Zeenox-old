using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Modules.Music;
using Google.Apis.YouTube.v3;
using Lavalink4NET;
using Lavalink4NET.Cluster;
using Lavalink4NET.Events;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Microsoft.Extensions.Caching.Memory;

namespace Discordance.Services;

public class AudioService
{
    private readonly IAudioService _audioService;
    private readonly YouTubeService _youtubeService;
    private readonly IMemoryCache _cache;
    private readonly LocalizationService _localizationService;

    public AudioService(
        IAudioService lavaNode,
        YouTubeService youtubeService,
        DiscordShardedClient client,
        IMemoryCache cache,
        LocalizationService localizationService
    )
    {
        _audioService = lavaNode;
        _audioService.TrackEnd += OnTrackEnd;
        _youtubeService = youtubeService;
        _cache = cache;
        _localizationService = localizationService;
        client.MessageReceived += ListenForSongRequests;
        foreach (var node in ((LavalinkCluster)_audioService).Nodes)
        {
            node.UseSponsorBlock();
        }
    }

    private async Task ListenForSongRequests(SocketMessage arg)
    {
        if (
            arg is not SocketUserMessage message
            || message.Author.IsBot
            || message.Author.IsWebhook
            || message.Author is not SocketGuildUser user
            || message.Channel is not SocketTextChannel channel
        )
        {
            return;
        }

        var guild = channel.Guild;
        var config = _cache.GetGuildConfig(guild.Id).Music;

        if (user.VoiceChannel is null)
            return;

        if (config.RequestChannelId is null)
            return;

        if (message.Channel.Id != config.RequestChannelId)
            return;

        var player = await GetOrCreatePlayerAsync(user).ConfigureAwait(false);

        var (tracks, isPlaylist) = await SearchAsync(message.Content, user).ConfigureAwait(false);
        if (tracks is null)
            return;

        var track = tracks[0];

        await message.DeleteAsync().ConfigureAwait(false);
        if (player.TextChannel.Id != config.RequestChannelId)
        {
            await player.Message.DeleteAsync().ConfigureAwait(false);
            player.SetMessage(
                await channel
                    .SendMessageAsync(
                        embed: player.GetNowPlayingEmbed(track),
                        components: player.GetMessageComponents()
                    )
                    .ConfigureAwait(false)
            );
        }

        if (isPlaylist && config.PlaylistAllowed)
        {
            await player.PlayPlaylistAsync(user, tracks).ConfigureAwait(false);
            return;
        }

        await player.PlayAsync(user, track).ConfigureAwait(false);
    }

    private async Task OnTrackEnd(object _, TrackEndEventArgs eventArgs)
    {
        var player = (DiscordancePlayer)eventArgs.Player;

        if (!eventArgs.MayStartNext)
            return;

        if (player.IsLooping)
            return;

        var previous = player.CurrentTrack!;
        player.History.Add(previous);

        if (player.Queue.Count > 0)
            return;

        if (player.IsAutoPlay)
        {
            var next = await GetRelatedVideoId(player.CurrentTrack!.TrackIdentifier)
                .ConfigureAwait(false);

            var track = await _audioService
                .GetTrackAsync($"https://www.youtube.com/watch?v={next}")
                .ConfigureAwait(false);
            track!.Context = previous.Context;
            await player.PlayAsync((IUser)previous.Context, track).ConfigureAwait(false);
            return;
        }

        await player.WaitForInputAsync().ConfigureAwait(false);
    }

    public bool IsPlaying(ulong guildId, out DiscordancePlayer? player)
    {
        player = _audioService.GetPlayer<DiscordancePlayer>(guildId);
        return player is not null;
    }

    public async Task<DiscordancePlayer> GetOrCreatePlayerAsync(SocketGuildUser user)
    {
        var voiceChannel = user.VoiceChannel;
        var guild = user.Guild;
        var config = _cache.GetGuildConfig(guild.Id);
        if (_audioService.GetPlayer<DiscordancePlayer>(guild.Id) is { } player)
            return player;
        player = await _audioService
            .JoinAsync(
                () =>
                    new DiscordancePlayer(
                        voiceChannel,
                        config.Language,
                        config.Music.ShowRequester,
                        _localizationService
                    ),
                guild.Id,
                voiceChannel.Id
            )
            .ConfigureAwait(false);
        if (config.Music.UseSponsorBlock)
            player.GetCategories().Add(SegmentCategory.OfftopicMusic);
        if (config.Music.DefaultVolume != 100)
            await player.SetVolumeAsync(config.Music.DefaultVolume).ConfigureAwait(false);
        return player;
    }

    public async Task SkipOrVoteskipAsync(IUser user, IGuild guild, ulong userId)
    {
        var player = GetPlayer(guild.Id);
        if (player.RequestedBy.Id != userId)
            await player.VoteSkipAsync(user).ConfigureAwait(false);
        else
            await player.SkipAsync(user).ConfigureAwait(false);
    }

    public async Task PauseOrResumeAsync(IGuild guild, IUser user)
    {
        var player = GetPlayer(guild.Id);
        switch (player.State)
        {
            case PlayerState.Playing:
            {
                await player.PauseAsync(user).ConfigureAwait(false);
                break;
            }
            case PlayerState.Paused:
            {
                await player.ResumeAsync(user).ConfigureAwait(false);
                break;
            }
        }
    }

    public async Task SetFiltersAsync(IGuild guild, SocketUser user, FilterType filterType)
    {
        var player = GetPlayer(guild.Id);
        if (filterType is not FilterType.None)
            player.Filters.Clear();
        player.Filters.ApplyFilter(filterType);
        await player.Filters.CommitAsync().ConfigureAwait(false);
        await player.SetFilterNameAsync(user, filterType.GetDescription()!).ConfigureAwait(false);
    }

    public DiscordancePlayer GetPlayer(ulong guildId) =>
        _audioService.GetPlayer<DiscordancePlayer>(guildId)!;

    private async Task<string> GetRelatedVideoId(string videoId)
    {
        var searchListRequest = _youtubeService.Search.List("snippet");
        searchListRequest.RelatedToVideoId = videoId;
        searchListRequest.Type = "video";
        searchListRequest.MaxResults = 10;
        var result = await searchListRequest.ExecuteAsync().ConfigureAwait(false);
        return result.Items.First(x => x.Snippet is not null).Id.VideoId;
    }

    public async Task<(LavalinkTrack[]? tracks, bool isPlaylist)> SearchAsync(
        string query,
        IUser user
    )
    {
        var results = Uri.IsWellFormedUriString(query, UriKind.Absolute)
          ? await _audioService.LoadTracksAsync(query).ConfigureAwait(false)
          : await _audioService.LoadTracksAsync(query, SearchMode.YouTube).ConfigureAwait(false);

        var tracks = results.Tracks;
        if (tracks is null || tracks.Length == 0)
            return (null, false);
        foreach (var track in tracks)
            track.Context = user;
        return (tracks, results.PlaylistInfo?.Name is not null);
    }

    public ValueTask<LavalinkTrack?> GetTrackAsync(string url) =>
        _audioService.GetTrackAsync(url, SearchMode.YouTube);
}
