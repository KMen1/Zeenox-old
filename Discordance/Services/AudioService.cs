using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Enums;
using Discordance.Extensions;
using Discordance.Models;
using Discordance.Models.Socket.Server;
using Discordance.Modules.Music;
using Google.Apis.YouTube.v3;
using Lavalink4NET;
using Lavalink4NET.Events;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Integrations.SponsorBlock.Event;
using Lavalink4NET.Player;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.Caching.Memory;

namespace Discordance.Services;

public sealed class AudioService
{
    public enum SearchMode
    {
        None,
        YouTube,
        Spotify
    }

    private readonly IMemoryCache _cache;
    private readonly LavalinkNode _lavalinkNode;
    private readonly Random _random = new();
    private readonly SearchService _searchService;
    private readonly YouTubeService _youTubeService;
    public readonly SocketHelper SocketHelper;

    public AudioService(
        IAudioService lavalinkNode,
        YouTubeService youtubeService,
        DiscordShardedClient client,
        IMemoryCache cache,
        InactivityTrackingService trackingService, SearchService searchService)
    {
        _lavalinkNode = (LavalinkNode) lavalinkNode;
        _lavalinkNode.TrackEnd += OnTrackEnd;
        _cache = cache;
        _searchService = searchService;
        SocketHelper = new SocketHelper(this, searchService, client);
        _youTubeService = youtubeService;
        client.MessageReceived += ListenForSongRequests;
        _lavalinkNode.UseSponsorBlock();
        lavalinkNode.Integrations.Get<ISponsorBlockIntegration>()!.SegmentsLoaded += OnSegmentsLoaded;
        trackingService.BeginTracking();
        trackingService.InactivePlayer += OnInactivePlayer;
    }

    public MusicPlayer? GetPlayer(ulong guildId)
    {
        return _lavalinkNode.GetPlayer<MusicPlayer>(guildId);
    }

    public async Task<MusicPlayer> CreatePlayerAsync(
        ulong guildId,
        IVoiceChannel voiceChannel,
        ITextChannel textChannel
    )
    {
        if (GetPlayer(guildId) is { } player)
            return player;

        var config = _cache.GetGuildConfig(guildId);

        player = await _lavalinkNode.JoinAsync(
                () => new MusicPlayer(voiceChannel, textChannel, config.Language, _cache),
                guildId,
                voiceChannel.Id
            )
            .ConfigureAwait(false);
        if (config.Music.UseSponsorBlock)
            player.GetCategories().AddAll();
        if (config.Music.DefaultVolume != 100)
            await player.SetVolumeAsync(config.Music.DefaultVolume).ConfigureAwait(false);
        return player;
    }

    public async Task PlayAsync(
        ulong guildId,
        IUser user,
        LavalinkTrack track,
        bool force = false
    )
    {
        var player = GetPlayer(guildId)!;
        if (force)
        {
            await player.PlayAsync(track, false).ConfigureAwait(false);
            await SocketHelper.SendMessageAsync(guildId,
                    ServerMessageType.UpdateCurrentTrack | ServerMessageType.UpdateQueue,
                    player)
                .ConfigureAwait(false);
            return;
        }

        player.AppendAction(
            _cache
                .GetMessage(player.Language, "TrackAdded")
                .FormatWithTimestamp(user.Mention, track.ToHyperLink())
        );
        var position = await player.PlayAsync(track).ConfigureAwait(false);
        if (position != 0)
            await SocketHelper
                .SendMessageAsync(guildId, ServerMessageType.UpdateQueue, player)
                .ConfigureAwait(false);
        await SocketHelper
            .SendMessageAsync(guildId, ServerMessageType.UpdateCurrentTrack, player)
            .ConfigureAwait(false);
    }

    public async Task PlayAsync(
        ulong guildId,
        IUser user,
        LavalinkTrack[] tracks
    )
    {
        var player = GetPlayer(guildId)!;
        player.AppendAction(
            _cache
                .GetMessage(player.Language, "PlaylistAdded")
                .FormatWithTimestamp(user.Mention, tracks.Length)
        );
        tracks = await _searchService.AddCoverUrls(tracks).ConfigureAwait(false);
        await player.PlayAsync(tracks).ConfigureAwait(false);
        await SocketHelper.SendMessageAsync(guildId,
                ServerMessageType.UpdateQueue | ServerMessageType.UpdateCurrentTrack, player)
            .ConfigureAwait(false);
    }

    public async Task SkipAsync(ulong guildId, IUser user)
    {
        var player = GetPlayer(guildId)!;

        /*if (player.IsAutoPlay)
        {
            var track = await GetRelatedTrackAsync(player).ConfigureAwait(false);
            await player.PlayAsync(track, false).ConfigureAwait(false);
            await SocketHelper.SendMessageAsync(guildId, ServerMessage.FromPlayer(ServerMessageType.SetTrack, player))
                .ConfigureAwait(false);
            return;
        }*/

        if (player.RequestedBy.Id != user.Id)
        {
            var result = await player.VoteAsync(user.Id).ConfigureAwait(false);
            if (!result.WasAdded)
                return;

            if (!result.WasSkipped)
            {
                player.AppendAction(
                    _cache
                        .GetMessage(player.Language, "VoteSkip")
                        .FormatWithTimestamp(user.Mention, (player.VoteSkipRequired - player.VoteSkipCount).ToString())
                );
                //await player.UpdateMessageAsync().ConfigureAwait(false);
                return;
            }

            player.AppendAction(
                _cache
                    .GetMessage(player.Language, "VoteSkipped")
                    .FormatWithTimestamp(player.CurrentTrack!.ToHyperLink())
            );
            //await player.UpdateMessageAsync().ConfigureAwait(false);
            await SocketHelper.SendMessageAsync(guildId, ServerMessageType.UpdateCurrentTrack, player)
                .ConfigureAwait(false);
            return;
        }

        player.Queue[0].Context = (TrackContext) player.Queue[0].Context! with
        {
            CoverUrl = await _searchService.GetCoverUrl(player.Queue[0]).ConfigureAwait(false)
        };
        await player.SkipAsync().ConfigureAwait(false);
        player.AppendAction(
            _cache
                .GetMessage(player.Language, "Skipped")
                .FormatWithTimestamp(user.Mention, player.CurrentTrack!.ToHyperLink())
        );
        //await player.UpdateMessageAsync().ConfigureAwait(false);
        await SocketHelper.SendMessageAsync(guildId,
                ServerMessageType.UpdateCurrentTrack | ServerMessageType.UpdateQueue, player)
            .ConfigureAwait(false);
    }

    public async Task RewindAsync(ulong guildId, IUser user)
    {
        var player = GetPlayer(guildId)!;
        player.AppendAction(
            _cache.GetMessage(player.Language, "Previous").FormatWithTimestamp(user.Mention)
        );
        await player.RewindAsync().ConfigureAwait(false);
        await SocketHelper
            .SendMessageAsync(guildId, ServerMessageType.UpdateCurrentTrack, player)
            .ConfigureAwait(false);
    }

    public async Task<bool> PauseOrResumeAsync(ulong guildId, IUser user)
    {
        var player = GetPlayer(guildId)!;
        switch (player.State)
        {
            case PlayerState.Playing:
            {
                player.AppendAction(
                    _cache.GetMessage(player.Language, "Paused").FormatWithTimestamp(user.Mention)
                );
                await player.PauseAsync().ConfigureAwait(false);
                break;
            }
            case PlayerState.Paused:
            {
                player.AppendAction(
                    _cache
                        .GetMessage(player.Language, "Resumed")
                        .FormatWithTimestamp(user.Mention)
                );
                await player.ResumeAsync().ConfigureAwait(false);
                break;
            }
        }

        await SocketHelper
            .SendMessageAsync(guildId, ServerMessageType.UpdatePlayerStatus, player)
            .ConfigureAwait(false);
        return player.State == PlayerState.Paused;
    }

    public async Task SetFilterAsync(ulong guildId, IUser user, FilterType filterType)
    {
        var player = GetPlayer(guildId)!;
        player.CurrentFilter = _cache.GetMessage(
            player.Language,
            $"Filter{filterType.ToString()}"
        );
        player.AppendAction(
            _cache
                .GetMessage(player.Language, "SetFilter")
                .FormatWithTimestamp(user.Mention, filterType)
        );
        await player.ApplyFiltersAsync(filterType).ConfigureAwait(false);
    }

    public async Task<int> SetVolumeAsync(ulong guildId, IUser user, float volume)
    {
        var player = GetPlayer(guildId)!;
        player.AppendAction(
            _cache
                .GetMessage(player.Language, "VolumeAction")
                .FormatWithTimestamp(user.Mention, Math.Round(volume * 100).ToString())
        );
        await player.SetVolumeAsync(volume).ConfigureAwait(false);
        await SocketHelper
            .SendMessageAsync(guildId, ServerMessageType.UpdatePlayerStatus, player)
            .ConfigureAwait(false);
        return (int) (volume * 100);
    }

    public async Task<int> ClearQueueAsync(ulong guildId, IUser user)
    {
        var player = GetPlayer(guildId)!;
        player.AppendAction(
            _cache.GetMessage(player.Language, "QueueClearedAction").FormatWithTimestamp(user.Mention)
        );
        var count = await player.ClearQueueAsync().ConfigureAwait(false);
        await SocketHelper.SendMessageAsync(guildId, ServerMessageType.UpdateQueue, player)
            .ConfigureAwait(false);
        return count;
    }

    public async Task<PlayerLoopMode> ToggleLoopAsync(ulong guildId, IUser user)
    {
        var player = GetPlayer(guildId)!;
        player.AppendAction(
            _cache
                .GetMessage(player.Language, player.LoopMode != PlayerLoopMode.None ? "LoopEnabled" : "LoopDisabled")
                .FormatWithTimestamp(user.Mention)
        );
        var shouldDisable = !Enum.IsDefined(typeof(PlayerLoopMode), player.LoopMode + 1);
        await player.ToggleLoopAsync(shouldDisable ? 0 : player.LoopMode + 1).ConfigureAwait(false);
        await SocketHelper
            .SendMessageAsync(guildId, ServerMessageType.UpdatePlayerStatus, player)
            .ConfigureAwait(false);
        return player.LoopMode;
    }

    public async Task<bool> ToggleAutoPlayAsync(ulong guildId, IUser user)
    {
        var player = GetPlayer(guildId)!;
        player.AppendAction(
            _cache
                .GetMessage(
                    player.Language,
                    !player.IsAutoPlay ? "AutoplayEnabled" : "AutoplayDisabled"
                )
                .FormatWithTimestamp(user.Mention)
        );
        await player.ToggleAutoPlayAsync().ConfigureAwait(false);
        await SocketHelper
            .SendMessageAsync(guildId, ServerMessageType.UpdatePlayerStatus, player)
            .ConfigureAwait(false);
        return player.IsAutoPlay;
    }

    private async Task ListenForSongRequests(SocketMessage arg)
    {
        if (
            arg is not SocketUserMessage message
            || message.Author.IsBot
            || message.Author.IsWebhook
            || message.Author is not SocketGuildUser user
            || user.VoiceChannel is null
            || message.Channel is not SocketTextChannel channel
        )
            return;

        var guild = channel.Guild;
        var config = _cache.GetGuildConfig(guild.Id).Music;

        if (config.RequestChannelId is null)
            return;

        if (channel.Id != config.RequestChannelId)
            return;

        var player = GetPlayer(guild.Id) ?? await CreatePlayerAsync(guild.Id, user.VoiceChannel, channel)
            .ConfigureAwait(false);

        var tracks = await _searchService.SearchAsync(message.Content, user, SearchMode.YouTube).ConfigureAwait(false);
        if (tracks.Length == 0)
            return;

        var track = tracks[0];

        await message.DeleteAsync().ConfigureAwait(false);

        if (tracks.Length > 1 && config.PlaylistAllowed)
        {
            await PlayAsync(player.GuildId, user, tracks).ConfigureAwait(false);
            return;
        }

        await PlayAsync(player.GuildId, user, track).ConfigureAwait(false);
    }

    private async Task<string> GetRelatedVideoFromYoutube(string videoId)
    {
        var request = _youTubeService.Search.List("snippet");
        request.MaxResults = 5;
        request.RelatedToVideoId = videoId;
        request.Type = "video";
        var result = await request.ExecuteAsync().ConfigureAwait(false);
        var availableVideos = result.Items.Where(x => x.Snippet is not null).ToArray();
        var id = availableVideos[_random.Next(0, availableVideos.Length)].Id.VideoId;
        return $"https://www.youtube.com/watch?v={id}";
    }

    private async Task OnTrackEnd(object _, TrackEndEventArgs eventArgs)
    {
        var player = (MusicPlayer) eventArgs.Player;
        player.ClearVotes();
        player.SponsorBlockSkipTime = null;

        var previous = player.CurrentTrack;
        if (player.LoopMode == PlayerLoopMode.None && previous is not null)
            player.History.Add(previous);

        if (eventArgs.Reason == TrackEndReason.Replaced)
            return;

        if (eventArgs.MayStartNext && player.Queue.Count > 0)
        {
            player.Queue[0].Context = (TrackContext) player.Queue[0].Context! with
            {
                CoverUrl = await _searchService.GetCoverUrl(player.Queue[0]).ConfigureAwait(false)
            };
            await player.SkipAsync().ConfigureAwait(false);
            await SocketHelper.SendMessageAsync(player.GuildId,
                    ServerMessageType.UpdateCurrentTrack | ServerMessageType.UpdateQueue,
                    player, UpdateQueueMessageType.Remove)
                .ConfigureAwait(false);
            return;
        }

        /*if (player.IsAutoPlay)
        {
            var track = await GetRelatedTrackAsync(player).ConfigureAwait(false);
            await player.PlayAsync(track!, false).ConfigureAwait(false);
            await SocketHelper
                .SendMessageAsync(player.GuildId, ServerMessage.FromPlayer(ServerMessageType.SetTrack, player))
                .ConfigureAwait(false);
        }*/

        await player.StopAsync().ConfigureAwait(false);
        var message = await player.GetMessage().ConfigureAwait(false);
        await message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithDescription(
                            _cache.GetMessage(player.Language, "PlayerWaiting")
                        )
                        .WithColor(new Color(31, 31, 31))
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
    }

    private static async Task OnInactivePlayer(object sender, InactivePlayerEventArgs eventargs)
    {
        if (!eventargs.ShouldStop) return;
        var player = (MusicPlayer) eventargs.Player;
        var msg = await player.GetMessage().ConfigureAwait(false);
        await msg.DeleteAsync().ConfigureAwait(false);
    }

    private static Task OnSegmentsLoaded(object _, SegmentsLoadedEventArgs eventargs)
    {
        var player = (MusicPlayer) eventargs.Player!;
        var segments = eventargs.Segments;
        var totalDurationMs = segments.Sum(s => (s.EndOffset - s.StartOffset).TotalMilliseconds);
        player.SponsorBlockSkipTime = TimeSpan.FromMilliseconds(totalDurationMs);
        return Task.CompletedTask;
    }
}