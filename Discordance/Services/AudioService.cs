using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Models;
using Discordance.Modules.Music;
using Google.Apis.YouTube.v3;
using Lavalink4NET;
using Lavalink4NET.Cluster;
using Lavalink4NET.Events;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;

namespace Discordance.Services;

public class AudioService
{
    private readonly Dictionary<FilterType, Action<PlayerFilterMap>> _filterActions =
        new()
        {
            { FilterType.None, x => x.Clear() },
            { FilterType.Bassboost, x => x.BassBoost() },
            { FilterType.Pop, x => x.Pop() },
            { FilterType.Soft, x => x.Soft() },
            { FilterType.Treblebass, x => x.Treblebass() },
            { FilterType.Nightcore, x => x.Nightcore() },
            { FilterType.Eightd, x => x.Eightd() },
            { FilterType.Vaporwave, x => x.Vaporwave() },
            { FilterType.Doubletime, x => x.Doubletime() },
            { FilterType.Slowmotion, x => x.Slowmotion() },
            { FilterType.Chipmunk, x => x.Chipmunk() },
            { FilterType.Darthvader, x => x.Darthvader() },
            { FilterType.Dance, x => x.Dance() },
            { FilterType.China, x => x.China() },
            { FilterType.Vibrato, x => x.Vibrato() },
            { FilterType.Tremolo, x => x.Tremolo() }
        };

    private readonly IAudioService _audioService;
    private readonly YouTubeService _youtubeService;
    private readonly MongoService _mongoService;

    public AudioService(
        IAudioService lavaNode,
        YouTubeService youtubeService,
        DiscordShardedClient client,
        MongoService mongoService
    )
    {
        _audioService = lavaNode;
        _audioService.TrackEnd += OnTrackEnd;
        _youtubeService = youtubeService;
        _mongoService = mongoService;
        client.MessageReceived += ListenForSongRequests;
        foreach (var node in ((LavalinkCluster)_audioService).Nodes)
        {
            node.UseSponsorBlock();
        }
    }

    public async Task<MusicConfig> GetConfig(ulong guildId)
    {
        return (await _mongoService.GetGuildConfigAsync(guildId).ConfigureAwait(false)).Music;
    }

    private async Task ListenForSongRequests(SocketMessage arg)
    {
        if (
            arg is not SocketUserMessage message
            || message.Author.IsBot
            || message.Author.IsWebhook
            || message.Author is not SocketGuildUser user
        )
            return;

        if (message.Channel is not SocketTextChannel channel)
            return;

        var guild = channel.Guild;
        var config = (
            await _mongoService.GetGuildConfigAsync(guild.Id).ConfigureAwait(false)
        ).Music;

        if (config.RequestChannelId is null)
            return;

        if (message.Channel.Id != config.RequestChannelId)
            return;

        if (user.VoiceChannel is null)
            return;

        var player = await GetOrCreatePlayerAsync(user).ConfigureAwait(false);

        var (tracks, isPlaylist) = await SearchAsync(message.Content, user).ConfigureAwait(false);
        var track = tracks[0];

        await message.DeleteAsync().ConfigureAwait(false);
        if (player.Message is not null && player.Message.Channel.Id != config.RequestChannelId)
        {
            await player.Message.DeleteAsync().ConfigureAwait(false);
        }

        if (!player.IsPlaying)
        {
            player.Message = await channel
                .SendMessageAsync(embed: player.Embed(track), components: player.Components())
                .ConfigureAwait(false);
        }

        if (isPlaylist && config.PlaylistAllowed)
        {
            await player.PlayOrEnqueueAsync(user, tracks, isPlaylist).ConfigureAwait(false);
            return;
        }

        await player.PlayAsync(user, track).ConfigureAwait(false);
    }

    private async Task OnTrackEnd(object _, TrackEndEventArgs eventArgs)
    {
        if (eventArgs.Player is not DiscordancePlayer player)
            return;

        if (!eventArgs.MayStartNext)
            return;

        if (player.IsLooping)
            return;

        var previous = player.CurrentTrack!;
        player.History.Add(previous);
        player.SkipVotesNeeded = player.VoiceChannel.ConnectedUsers.Count(x => !x.IsBot) / 2;

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
        if (_audioService.GetPlayer<DiscordancePlayer>(guild.Id) is { } player)
            return player;
        player = await _audioService
            .JoinAsync(() => new DiscordancePlayer(voiceChannel), guild.Id, voiceChannel.Id)
            .ConfigureAwait(false);
        var config = await _mongoService.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        var musicConfig = config.Music;
        if (musicConfig.UseSponsorBlock)
            player.GetCategories().Add(SegmentCategory.OfftopicMusic);
        if (musicConfig.DefaultVolume != 100)
            await player.SetVolumeAsync(musicConfig.DefaultVolume).ConfigureAwait(false);
        player.ShowRequester = musicConfig.ShowRequester;
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

    public async Task<Embed?> PauseOrResumeAsync(IGuild guild, IUser user)
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

        return null;
    }

    public async Task SetFiltersAsync(IGuild guild, SocketUser user, FilterType filterType)
    {
        var player = GetPlayer(guild.Id);
        if (filterType is not FilterType.None)
            player.Filters.Clear();
        _filterActions[filterType](player.Filters);
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
        if (tracks != null)
        {
            foreach (var track in tracks)
                track.Context = user;
        }
        return results.Tracks is not null && results.Tracks.Length > 0
          ? (tracks, results.PlaylistInfo?.Name is not null)
          : (null, false);
    }

    public ValueTask<LavalinkTrack?> GetTrackAsync(string url)
    {
        return _audioService.GetTrackAsync(url, SearchMode.YouTube);
    }
}
