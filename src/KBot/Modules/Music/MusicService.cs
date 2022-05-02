using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using Humanizer;
using KBot.Enums;
using KBot.Extensions;
using KBot.Modules.Music.Embeds;
using Lavalink4NET;
using Lavalink4NET.Logging;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Serilog;

namespace KBot.Modules.Music;

public class MusicService : IInjectable
{
    private readonly Dictionary<FilterTypes, Action<PlayerFilterMap>> _filterActions = new()
    {
        {FilterTypes.None, x => x.Clear()},
        {FilterTypes.Bassboost, x => x.EnableBassBoost()},
        {FilterTypes.Pop, x => x.EnablePop()},
        {FilterTypes.Soft, x => x.EnableSoft()},
        {FilterTypes.Treblebass, x => x.EnableTreblebass()},
        {FilterTypes.Nightcore, x => x.EnableNightcore()},
        {FilterTypes.Eightd, x => x.EnableEightd()},
        {FilterTypes.Vaporwave, x => x.EnableVaporwave()},
        {FilterTypes.Doubletime, x => x.EnableDoubletime()},
        {FilterTypes.Slowmotion, x => x.EnableSlowmotion()},
        {FilterTypes.Chipmunk, x => x.EnableChipmunk()},
        {FilterTypes.Darthvader, x => x.EnableDarthvader()},
        {FilterTypes.Dance, x => x.EnableDance()},
        {FilterTypes.China, x => x.EnableChina()},
        {FilterTypes.Vibrato, x => x.EnableVibrato()},
        {FilterTypes.Tremolo, x => x.EnableTremolo()}
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
        return new EmbedBuilder().WithDescription($"**Successfully left {player.VoiceChannel.Mention}**")
            .WithColor(Color.Green)
            .Build();
    }

    public async Task<Embed> MoveAsync(IGuild guild, IVoiceChannel channel)
    {
        if (!_lavaNode.HasPlayer(guild.Id))
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        var player = _lavaNode.GetPlayer(guild.Id);
        await player!.ConnectAsync(channel.Id).ConfigureAwait(false);
        return new EmbedBuilder().WithDescription($"**Succesfully moved to {channel.Mention}**")
            .WithColor(Color.Green)
            .Build();
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
                return new AddedToQueueEmbedBuilder(searchResponse.Tracks).Build();
            }

            searchResponse.Tracks![0].Context = user;
            await player.EnqueueAsync(searchResponse.Tracks![0]).ConfigureAwait(false);
            return new AddedToQueueEmbedBuilder(new List<LavalinkTrack> {searchResponse.Tracks[0]}).Build();
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
            return new AddedToQueueEmbedBuilder(new List<LavalinkTrack> {track}).Build();
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
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder()
                .WithColor(Color.Red)
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
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();
        var currentVolume = player.Volume;
        if ((currentVolume == 0f && buttonType == VoiceButtonType.VolumeDown) ||
            (currentVolume == 1f && buttonType == VoiceButtonType.VolumeUp))
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Volume must be between 0 and 100!").Build();
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
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Not currently playing in this server!").Build();
        await player.SetVolumeAsync(volume).ConfigureAwait(false);
        return new EmbedBuilder()
            .WithDescription($"**Successfully set volume to {player.Volume.ToString(CultureInfo.InvariantCulture)}**")
            .WithColor(Color.Green)
            .Build();
    }

    public async Task<Embed?> SetFiltersAsync(IGuild guild, SocketUser user, FilterTypes filterType)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Not currently playing in this server!").Build();
        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder()
                .WithColor(Color.Red)
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
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();

        await player.SetFilterAsync(FilterTypes.None, _filterActions[FilterTypes.None]).ConfigureAwait(false);
        return null;
    }

    public Embed GetQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
        {
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Not currently playing in this server!")
                .Build();
        }

        var eb = new EmbedBuilder();
        eb.WithTitle("Current queue").WithColor(Color.Green);
        if (player.QueueCount == 0)
        {
            eb.WithDescription("`No songs in queue`");
        }
        else
        {
            var desc = player.Queue.Aggregate("",
                (current, track) =>
                    current +
                    $":{(player.Queue.TakeWhile(n => n != track).Count() + 1).ToWords()}: [`{track.Title}`]({track.Source}) | Added by: {((SocketUser) track.Context!).Mention}\n");

            eb.WithDescription(desc);
        }
        return eb.Build();
    }

    public async Task<Embed> ClearQueueAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Not currently playing in this server!").Build();
        await player.ClearQueueAsync().ConfigureAwait(false);
        return new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription("**Queue cleared**")
            .Build();
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