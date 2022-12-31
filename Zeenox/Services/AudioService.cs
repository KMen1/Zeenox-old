using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Events;
using Lavalink4NET.Player;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.Caching.Memory;
using Zeenox.Enums;
using Zeenox.Extensions;
using Zeenox.Models;
using Zeenox.Models.Events;
using Zeenox.Modules.Music;

namespace Zeenox.Services;

public sealed class AudioService
{
    private readonly IMemoryCache _cache;
    private readonly LavalinkNode _lavalinkNode;
    private readonly SearchService _searchService;

    public AudioService(
        IAudioService lavalinkNode,
        DiscordShardedClient client,
        IMemoryCache cache,
        InactivityTrackingService trackingService, SearchService searchService)
    {
        _lavalinkNode = (LavalinkNode) lavalinkNode;
        _lavalinkNode.TrackEnd += OnTrackEnd;
        _cache = cache;
        _searchService = searchService;
        client.MessageReceived += ListenForSongRequests;
        _lavalinkNode.UseSponsorBlock();
        trackingService.BeginTracking();
        trackingService.InactivePlayer += OnInactivePlayer;
    }

    public event AsyncEventHandler<PlayEventArgs>? OnPlayAsync;
    public event AsyncEventHandler<QueueChangedEventArgs>? OnQueueChangedAsync;
    public event AsyncEventHandler<TrackEndedEventArgs>? OnTrackEndedAsync;
    public event AsyncEventHandler<PlayerStatusChangedEventArgs>? OnPlayerStatusChangedAsync;

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
        LavalinkTrack track,
        bool force = false
    )
    {
        var player = GetPlayer(guildId)!;
        if (force)
        {
            await player.PlayAsync(track, false).ConfigureAwait(false);
            await OnPlayAsync.InvokeAsync(player, new PlayEventArgs(guildId, track)).ConfigureAwait(false);
            await OnQueueChangedAsync.InvokeAsync(player, new QueueChangedEventArgs(guildId, player.Queue.ToArray()))
                .ConfigureAwait(false);
            return;
        }

        var position = await player.PlayAsync(track).ConfigureAwait(false);
        if (position != 0)
        {
            await OnQueueChangedAsync.InvokeAsync(player, new QueueChangedEventArgs(guildId, player.Queue.ToArray()))
                .ConfigureAwait(false);
            return;
        }

        await OnPlayAsync.InvokeAsync(player, new PlayEventArgs(guildId, track)).ConfigureAwait(false);
    }

    public async Task PlayAsync(
        ulong guildId,
        LavalinkTrack[] tracks
    )
    {
        var player = GetPlayer(guildId)!;
        tracks = await _searchService.AddCoverUrls(tracks).ConfigureAwait(false);
        await player.PlayAsync(tracks).ConfigureAwait(false);
        await OnPlayAsync.InvokeAsync(player, new PlayEventArgs(guildId, tracks[0])).ConfigureAwait(false);
        await OnQueueChangedAsync.InvokeAsync(player, new QueueChangedEventArgs(guildId, player.Queue.ToArray()))
            .ConfigureAwait(false);
    }

    public async Task SkipAsync(ulong guildId, IUser user)
    {
        var player = GetPlayer(guildId)!;
        var requester = (player.CurrentTrack?.Context as TrackContext?)?.Requester;
        if (user.Id != requester?.Id)
        {
            var result = await player.VoteAsync(user.Id).ConfigureAwait(false);
            if (result.WasSkipped)
            {
                await OnPlayAsync.InvokeAsync(player, new PlayEventArgs(guildId, player.CurrentTrack!))
                    .ConfigureAwait(false);
                return;
            }
        }

        player.Queue[0].Context = (TrackContext) player.Queue[0].Context! with
        {
            CoverUrl = await _searchService.GetCoverUrl(player.Queue[0]).ConfigureAwait(false)
        };
        await player.SkipAsync().ConfigureAwait(false);
        await OnPlayAsync.InvokeAsync(player, new PlayEventArgs(guildId, player.CurrentTrack!)).ConfigureAwait(false);
        await OnQueueChangedAsync.InvokeAsync(player, new QueueChangedEventArgs(guildId, player.Queue.ToArray()))
            .ConfigureAwait(false);
    }

    public async Task RewindAsync(ulong guildId)
    {
        var player = GetPlayer(guildId)!;
        await player.RewindAsync().ConfigureAwait(false);
        await OnPlayAsync.InvokeAsync(player, new PlayEventArgs(guildId, player.CurrentTrack!)).ConfigureAwait(false);
    }

    public async Task<bool> PauseOrResumeAsync(ulong guildId)
    {
        var player = GetPlayer(guildId)!;
        switch (player.State)
        {
            case PlayerState.Playing:
            {
                await player.PauseAsync().ConfigureAwait(false);
                break;
            }
            case PlayerState.Paused:
            {
                await player.ResumeAsync().ConfigureAwait(false);
                break;
            }
        }

        await OnPlayerStatusChangedAsync.InvokeAsync(player, new PlayerStatusChangedEventArgs(guildId))
            .ConfigureAwait(false);
        return player.State == PlayerState.Paused;
    }

    public async Task SetFilterAsync(ulong guildId, FilterType filterType)
    {
        var player = GetPlayer(guildId)!;
        player.CurrentFilter = _cache.GetMessage(
            player.GuildId,
            $"Filter{filterType.ToString()}"
        );
        await player.ApplyFiltersAsync(filterType).ConfigureAwait(false);
    }

    public async Task<int> SetVolumeAsync(ulong guildId, float volume)
    {
        var player = GetPlayer(guildId)!;
        await player.SetVolumeAsync(volume).ConfigureAwait(false);
        await OnPlayerStatusChangedAsync.InvokeAsync(player, new PlayerStatusChangedEventArgs(guildId))
            .ConfigureAwait(false);
        return (int) (volume * 100);
    }

    public async Task<int> ClearQueueAsync(ulong guildId)
    {
        var player = GetPlayer(guildId)!;
        var count = await player.ClearQueueAsync().ConfigureAwait(false);
        await OnQueueChangedAsync.InvokeAsync(player, new QueueChangedEventArgs(guildId, player.Queue.ToArray()))
            .ConfigureAwait(false);
        return count;
    }

    public async Task<PlayerLoopMode> ToggleLoopAsync(ulong guildId)
    {
        var player = GetPlayer(guildId)!;
        var shouldDisable = !Enum.IsDefined(typeof(PlayerLoopMode), player.LoopMode + 1);
        await player.ToggleLoopAsync(shouldDisable ? 0 : player.LoopMode + 1).ConfigureAwait(false);
        await OnPlayerStatusChangedAsync.InvokeAsync(player, new PlayerStatusChangedEventArgs(guildId))
            .ConfigureAwait(false);
        return player.LoopMode;
    }

    public async Task<bool> ToggleAutoPlayAsync(ulong guildId)
    {
        var player = GetPlayer(guildId)!;
        await player.ToggleAutoPlayAsync().ConfigureAwait(false);
        await OnPlayerStatusChangedAsync.InvokeAsync(player, new PlayerStatusChangedEventArgs(guildId))
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
            await PlayAsync(player.GuildId, tracks).ConfigureAwait(false);
            return;
        }

        await PlayAsync(player.GuildId, track).ConfigureAwait(false);
    }

    private async Task OnTrackEnd(object _, TrackEndEventArgs eventArgs)
    {
        var player = (MusicPlayer) eventArgs.Player;
        player.ClearVotes();

        if (eventArgs.Reason == TrackEndReason.Replaced)
            return;

        if (eventArgs.MayStartNext && player.Queue.Count > 0)
        {
            player.Queue[0].Context = (TrackContext) player.Queue[0].Context! with
            {
                CoverUrl = await _searchService.GetCoverUrl(player.Queue[0]).ConfigureAwait(false)
            };
            await player.SkipAsync().ConfigureAwait(false);
            await OnTrackEndedAsync.InvokeAsync(player,
                    new TrackEndedEventArgs(player.GuildId, player.CurrentTrack!, player.Queue.ToArray()))
                .ConfigureAwait(false);
            return;
        }

        await player.StopAsync().ConfigureAwait(false);
        var message = await player.GetMessage().ConfigureAwait(false);
        await message
            .ModifyAsync(
                x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithDescription(
                            _cache.GetMessage(player.GuildId, "PlayerWaiting")
                        )
                        .WithColor(new Color(31, 31, 31))
                        .Build();
                    x.Components = new ComponentBuilder().Build();
                }
            )
            .ConfigureAwait(false);
        await OnPlayerStatusChangedAsync.InvokeAsync(player, new PlayerStatusChangedEventArgs(player.GuildId))
            .ConfigureAwait(false);
    }

    private async Task OnInactivePlayer(object sender, InactivePlayerEventArgs eventargs)
    {
        if (!eventargs.ShouldStop) return;
        var player = (MusicPlayer) eventargs.Player;
        var msg = await player.GetMessage().ConfigureAwait(false);
        await msg.DeleteAsync().ConfigureAwait(false);
        await OnPlayerStatusChangedAsync.InvokeAsync(player, new PlayerStatusChangedEventArgs(player.GuildId))
            .ConfigureAwait(false);
    }

    public Task SeekAsync(ulong guildId, int position)
    {
        var player = GetPlayer(guildId)!;
        return player.SeekPositionAsync(TimeSpan.FromSeconds(position));
    }

    public Task Shuffle(ulong guildId)
    {
        var player = GetPlayer(guildId)!;
        player.Queue.Shuffle();
        return OnQueueChangedAsync.InvokeAsync(player,
            new QueueChangedEventArgs(player.GuildId, player.Queue.ToArray()));
    }

    public Task RemoveDupes(ulong guildId)
    {
        var player = GetPlayer(guildId)!;
        player.Queue.Distinct();
        return OnQueueChangedAsync.InvokeAsync(player,
            new QueueChangedEventArgs(player.GuildId, player.Queue.ToArray()));
    }
}