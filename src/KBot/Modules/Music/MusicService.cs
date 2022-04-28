using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using KBot.Enums;
using KBot.Extensions;
using Lavalink4NET;
using Lavalink4NET.Logging;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Serilog;

namespace KBot.Modules.Music;

public class MusicService : IInjectable
{
    private readonly Dictionary<FilterType, Action<PlayerFilterMap>> _filterActions = new()
    {
        {FilterType.None, x => x.Clear()},
        {FilterType.Bassboost, x => x.EnableBassBoost()},
        {FilterType.Pop, x => x.EnablePop()},
        {FilterType.Soft, x => x.EnableSoft()},
        {FilterType.Treblebass, x => x.EnableTreblebass()},
        {FilterType.Nightcore, x => x.EnableNightcore()},
        {FilterType.Eightd, x => x.EnableEightd()},
        {FilterType.Vaporwave, x => x.EnableVaporwave()},
        {FilterType.Doubletime, x => x.EnableDoubletime()},
        {FilterType.Slowmotion, x => x.EnableSlowmotion()},
        {FilterType.Chipmunk, x => x.EnableChipmunk()},
        {FilterType.Darthvader, x => x.EnableDarthvader()},
        {FilterType.Dance, x => x.EnableDance()},
        {FilterType.China, x => x.EnableChina()},
        {FilterType.Vibrato, x => x.EnableVibrato()},
        {FilterType.Tremolo, x => x.EnableTremolo()}
    };

    private readonly LavalinkNode _lavaNode;
    private readonly YouTubeService _youtubeService;

    public MusicService(DiscordSocketClient client, LavalinkNode lavaNode, YouTubeService youtubeService)
    {
        _lavaNode = lavaNode;
        _youtubeService = youtubeService;
        ((EventLogger)_lavaNode.Logger!).LogMessage += LogLava;
        client.Ready += async () => await _lavaNode.InitializeAsync().ConfigureAwait(false);
    }

    private static void LogLava(object? sender, LogMessageEventArgs arg)
    {
        switch (arg.Level)
        {
            case LogLevel.Error:
                Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogLevel.Warning:
                Log.Logger.Error(arg.Exception, arg.Message);
                break;
            case LogLevel.Information:
                Log.Logger.Information(arg.Exception, arg.Message);
                break;
            case LogLevel.Trace:
                Log.Logger.Verbose(arg.Exception, arg.Message);
                break;
            case LogLevel.Debug:
                Log.Logger.Debug(arg.Exception, arg.Message);
                break;
            default:
                Log.Logger.Information(arg.Exception, arg.Message);
                break;
        }
    }

    public bool IsPlayingInGuild(IGuild guild)
    {
        if (!_lavaNode.HasPlayer(guild.Id)) return false;
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        return player!.IsPlaying;
    }

    public async Task<Embed> DisconnectAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild.Id))
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player!.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();
        await player.DisconnectAsync().ConfigureAwait(false);
        return new EmbedBuilder().LeaveEmbed(player.VoiceChannel);
    }

    public async Task<Embed> MoveAsync(IGuild guild, IVoiceChannel channel)
    {
        if (!_lavaNode.HasPlayer(guild.Id))
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        var player = _lavaNode.GetPlayer(guild.Id);
        await player!.ConnectAsync(channel.Id).ConfigureAwait(false);
        return new EmbedBuilder().MoveEmbed(channel);
    }

    public async Task<Embed?> PlayAsync(IGuild guild, SocketGuildUser user, IUserMessage message, string query)
    {
        var voiceChannel = user.VoiceChannel;
        var searchResponse = await SearchAsync(query).ConfigureAwait(false);
        if (searchResponse is null) return new EmbedBuilder().WithColor(Color.Red).WithTitle("No matches!").Build();
        var player = _lavaNode.HasPlayer(guild.Id)
            ? _lavaNode.GetPlayer<MusicPlayer>(guild.Id)
            : await _lavaNode
                .JoinAsync(() => new MusicPlayer(
                        voiceChannel,
                        message,
                        _youtubeService,
                        _lavaNode),
                    guild.Id,
                    voiceChannel.Id).ConfigureAwait(false);
        var isPlaylist = searchResponse.PlaylistInfo?.Name is not null;

        if (player!.IsPlaying)
        {
            if (isPlaylist)
            {
                foreach (var track in searchResponse.Tracks!) track.Context = user;
                await player.EnqueueAsync(searchResponse.Tracks).ConfigureAwait(false);
                return new EmbedBuilder().AddedToQueueEmbed(searchResponse.Tracks);
            }

            searchResponse.Tracks![0].Context = user;
            await player.EnqueueAsync(searchResponse.Tracks![0]).ConfigureAwait(false);
            return new EmbedBuilder().AddedToQueueEmbed(new List<LavalinkTrack> {searchResponse.Tracks[0]});
        }

        if (isPlaylist)
        {
            foreach (var track in searchResponse.Tracks!) track.Context = user;
            await player.PlayAsync(searchResponse.Tracks![0]).ConfigureAwait(false);
            await player.EnqueueAsync(searchResponse.Tracks.Skip(1)).ConfigureAwait(false);
            return null;
        }

        searchResponse.Tracks![0].Context = user;
        await player.PlayAsync(searchResponse.Tracks![0]).ConfigureAwait(false);
        return null;
    }

    public async Task<Embed?> PlayFromSearchAsync(IGuild guild, SocketGuildUser user, IUserMessage message, string id)
    {
        var voiceChannel = user.VoiceChannel;
        var track = await _lavaNode.GetTrackAsync("https://www.youtube.com/watch?v=" + id).ConfigureAwait(false);
        track!.Context = user;
        var player = _lavaNode.HasPlayer(guild.Id)
            ? _lavaNode.GetPlayer<MusicPlayer>(guild.Id)
            : await _lavaNode
                .JoinAsync(() => new MusicPlayer(
                        voiceChannel,
                        message,
                        _youtubeService,
                        _lavaNode), guild.Id,
                    voiceChannel.Id).ConfigureAwait(false);

        if (player!.IsPlaying)
        {
            await player.EnqueueAsync(track).ConfigureAwait(false);
            await message.DeleteAsync().ConfigureAwait(false);
            return new EmbedBuilder().AddedToQueueEmbed(new List<LavalinkTrack> {track});
        }

        await player.PlayAsync(track).ConfigureAwait(false);
        return null;
    }

    public async Task StopAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null) return;
        if (player.LastRequestedBy.Id != user.Id) return;
        await player.StopAsync().ConfigureAwait(false);
    }

    public async Task PlayNextTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null) return;
        if (player.LastRequestedBy.Id != user.Id)
            await player.VoteSkipAsync(user).ConfigureAwait(false);
        else
            await player.SkipAsync().ConfigureAwait(false);
    }

    public async Task PlayPreviousTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null || player.LastRequestedBy.Id != user.Id) return;
        await player.PlayPreviousAsync().ConfigureAwait(false);
    }

    public async Task<Embed?> PauseOrResumeAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();
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

        return null;
    }

    public async Task<Embed?> SetVolumeAsync(IGuild guild, SocketUser user, VoiceButtonType buttonType)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();
        var currentVolume = player.Volume;
        if ((currentVolume == 0f && buttonType == VoiceButtonType.VolumeDown) ||
            (currentVolume == 1f && buttonType == VoiceButtonType.VolumeUp))
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Volume must be between 0 and 100!").Build();
        switch (buttonType)
        {
            case VoiceButtonType.VolumeUp:
            {
                await player.SetVolumeAsync(currentVolume + 10 / 100f).ConfigureAwait(false);
                break;
            }
            case VoiceButtonType.VolumeDown:
            {
                await player.SetVolumeAsync(currentVolume - 10 / 100f).ConfigureAwait(false);
                break;
            }
        }

        return null;
    }

    public async Task<Embed> SetVolumeAsync(IGuild guild, ushort volume)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();
        await player.SetVolumeAsync(volume).ConfigureAwait(false);
        return new EmbedBuilder().VolumeEmbed(player);
    }

    public async Task<Embed?> SetFiltersAsync(IGuild guild, SocketUser user, FilterType filterType)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();
        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();
        await player.SetFilterAsync(filterType, _filterActions[filterType]).ConfigureAwait(false);
        return null;
    }

    public async Task ToggleRepeatAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null) return;
        await player.ToggleLoopAsync().ConfigureAwait(false);
    }

    public async Task<Embed?> ClearFiltersAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is not {State: PlayerState.Playing})
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();

        await player.SetFilterAsync(FilterType.None, _filterActions[FilterType.None]).ConfigureAwait(false);
        return null;
    }

    public Embed GetQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        return player is null
            ? new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build()
            : new EmbedBuilder().QueueEmbed(player);
    }

    public async Task<Embed> ClearQueueAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();
        await player.ClearQueueAsync().ConfigureAwait(false);
        return new EmbedBuilder().QueueEmbed(player, true);
    }

    public async Task<TrackLoadResponsePayload?> SearchAsync(string query)
    {
        var results = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.LoadTracksAsync(query).ConfigureAwait(false)
            : await _lavaNode.LoadTracksAsync(query, SearchMode.YouTube).ConfigureAwait(false);
        return results.Tracks is not null ? results : null;
    }

    public async Task ToggleAutoplayAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null) return;
        await player.ToggleAutoPlayAsync().ConfigureAwait(false);
    }
}