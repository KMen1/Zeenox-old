using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Helpers;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Filters;
using Victoria.Responses.Search;

// ReSharper disable InconsistentNaming

namespace KBot.Services;

public class AudioService
{
    private readonly DiscordSocketClient _client;
    private readonly LavaNode _lavaNode;
    
    private InteractionContext ctx;
    private readonly List<LavaTrack> previousTracks = new();
    private readonly List<string> enabledFilters = new();
    private LavaTrack currentTrack;
    private IUserMessage nowPlayingMessage;
    private bool isPlaying = true;
    private bool isloopEnabled;

    public AudioService(DiscordSocketClient client, LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
        _client = client;
    }

    public void InitializeAsync()
    {
        _client.Ready += OnReadyAsync;
        _lavaNode.OnTrackEnded += OnTrackEnded;
        _lavaNode.OnTrackException += OnTrackException;
    }

    private async Task OnReadyAsync()
    {
        await _lavaNode.ConnectAsync();
    }

    private async Task OnTrackException(TrackExceptionEventArgs arg)
    {
        await arg.Player.StopAsync();
        await arg.Player.ApplyFiltersAsync(FilterHelper.DefaultFilters());
        await arg.Player.TextChannel.SendMessageAsync(embed: await EmbedHelper.MakeError(_client.CurrentUser, arg.Exception.Message));
        await _lavaNode.LeaveAsync(arg.Player.VoiceChannel);
    }

    public async Task<Embed> JoinAsync(IGuild guild, ITextChannel tChannel, SocketUser user)
    {
        if (_lavaNode.HasPlayer(guild))
        {
            return await EmbedHelper.MakeError(user, "Már csatlakozva vagyok valahova!");
        }
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        if (voiceChannel is null)
        {
            return await EmbedHelper.MakeError(user, "Nem vagy hangcsatornában!");
        }
        await _lavaNode.JoinAsync(voiceChannel, tChannel);
        return await EmbedHelper.MakeJoin(user, voiceChannel);
    }

    public async Task<Embed> LeaveAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHelper.MakeError(user, "Ezen a szerveren nem található lejátszó!");
        }
        var player = _lavaNode.GetPlayer(guild);
        var voiceChannel = player.VoiceChannel;
        await _lavaNode.LeaveAsync(voiceChannel);
        return await EmbedHelper.MakeLeave(user, voiceChannel);
    }

    public async Task<Embed> MoveAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHelper.MakeError(user, "Ezen a szerveren nem található lejátszó!");
        }
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        if (voiceChannel is null)
        {
            return await EmbedHelper.MakeError(user, "Nem vagy hangcsatornában!");
        }
        var player = _lavaNode.GetPlayer(guild);
        await _lavaNode.MoveChannelAsync(voiceChannel);
        return await EmbedHelper.MakeMove(user, player, voiceChannel);
    }

    public async Task<(Embed, MessageComponent, bool)> PlayAsync(string query, IGuild guild,
        ITextChannel tChannel, SocketUser user, InteractionContext context)
    {
        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query)
            : await _lavaNode.SearchYouTubeAsync(query);
        if (search.Status == SearchStatus.NoMatches)
        {
            return (await EmbedHelper.MakeError(user, "Nincs találat!"), null, false);
        }
        var track = search.Tracks.FirstOrDefault();
        var voiceChannel = ((IVoiceState)user).VoiceChannel;
        if (voiceChannel is null) return (await EmbedHelper.MakeError(user, "Nem vagy hangcsatornában!"), null, false);
        var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(voiceChannel, tChannel);
        
        
        if (IsPlaying(player))
        {
            player.Queue.Enqueue(track);
            var newComponents = await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying);
            await ModifyNowPlayingMessage(null, newComponents);
            
            return (await EmbedHelper.MakeAddedToQueue(user, track, player), null, true);
        }

        ctx ??= context;
        await player.PlayAsync(track);
        await player.UpdateVolumeAsync(100);
        currentTrack = track;
        isPlaying = true;
        return (
            await EmbedHelper.MakeNowPlaying(user, player, isloopEnabled, player.Volume, enabledFilters),
            await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying), false);
    }

    public async Task StopAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null) return;
        
        await player.StopAsync();
        ctx = null;
        nowPlayingMessage = null;
        previousTracks.Clear();
        enabledFilters.Clear();
    }

    public async Task PlayNextTrack(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null || player.Queue.Count == 0)
        {
            return;
        }
        previousTracks.Add(currentTrack);
        await player.SkipAsync();
        
        var newEmbed = await EmbedHelper.MakeNowPlaying(user, player, isloopEnabled, player.Volume, enabledFilters);
        var newComponents = await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying);
        await ModifyNowPlayingMessage(newEmbed, newComponents);
    }

    public async Task PlayPreviousTrack(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        var prev = previousTracks.Last();
        await player.PlayAsync(prev);
        previousTracks.Remove(prev);
        
        var newEmbed = await EmbedHelper.MakeNowPlaying(user, player, isloopEnabled, player.Volume, enabledFilters);
        var newComponents = await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying);
        await ModifyNowPlayingMessage(newEmbed, newComponents);
    }

    public async Task PauseOrResumeAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null) return;

        switch (player.PlayerState)
        {
            case PlayerState.Playing:
            {
                await player.PauseAsync();
                isPlaying = false;
                var newComponents = await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying);
                await ModifyNowPlayingMessage(null, newComponents);
                break;
            }
            case PlayerState.Paused:
            {
                await player.ResumeAsync();
                isPlaying = true;
                var newComponents = await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying);
                await ModifyNowPlayingMessage(null, newComponents);
                break;
            }
        }
    }

    public async Task SetVolumeAsync(IGuild guild, VoiceButtonType buttonType)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null) return;
        
        var currentVolume = player.Volume;
        
        if (currentVolume == 0 && buttonType == VoiceButtonType.VolumeDown)
        {
            return;
        }
        if (currentVolume == 100 && buttonType == VoiceButtonType.VolumeUp)
        {
            return;
        }
        
        switch (buttonType)
        {
            case VoiceButtonType.VolumeUp:
            {
                var newVolume = currentVolume + 10;
                await player.UpdateVolumeAsync((ushort) newVolume);
                var newEmbed = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, newVolume, enabledFilters);
                await ModifyNowPlayingMessage(newEmbed, null);
                break;
            }
            case VoiceButtonType.VolumeDown:
            {
                var newVolume = currentVolume - 10;
                await player.UpdateVolumeAsync((ushort) newVolume);
                var newEmbed = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, newVolume, enabledFilters);
                await ModifyNowPlayingMessage(newEmbed, null);
                break;
            }
        }
    }
    
    public async Task<Embed> SetVolumeAsync(ushort volume, IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null) return await EmbedHelper.MakeError(user, "A lejátszó nem található!");

        await player.UpdateVolumeAsync(volume);
        var newEmbed1 = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, volume, enabledFilters);
        await ModifyNowPlayingMessage(newEmbed1, null);
        return await EmbedHelper.MakeVolume(user, player, volume);
    }

    public async Task<Embed> SetFiltersAsync(IGuild guild, SocketUser user, IFilter[] filters, EqualizerBand[] bands, string[] filtersName)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null) 
            return await EmbedHelper.MakeError(user, "A lejátszó nem található!");
        await player.ApplyFiltersAsync(filters, equalizerBands:bands);
        enabledFilters.Clear();
        enabledFilters.AddRange(filtersName);
        var newEmbed = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, player.Volume, enabledFilters);
        await ModifyNowPlayingMessage(newEmbed, null);
        return await EmbedHelper.MakeFilter(user, filtersName);
    }
    
    public async Task SetRepeatAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        isloopEnabled = !isloopEnabled;
        var newEmbed = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, player.Volume, enabledFilters);
        var newComponents =
            await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying);
        await ModifyNowPlayingMessage(newEmbed, newComponents);
    }

    public async Task<Embed> SetSpeedAsync(float value, IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
            return await EmbedHelper.MakeError(user, "Jelenleg nincs zene lejátszás alatt!");
        await player.ApplyFilterAsync(FilterHelper.Speed(value));
        enabledFilters.Add($"Speed: {value}");
        var newEmbed = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, player.Volume, enabledFilters);
        await ModifyNowPlayingMessage(newEmbed, null);
        return await EmbedHelper.MakeFilter(user, new [] {$"SEBESSÉG -> {value}"});
    }

    public async Task<Embed> SetPitchAsync(float value, IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
            return await EmbedHelper.MakeError(user, "Jelenleg nincs zene lejátszás alatt!");
        await player.ApplyFilterAsync(FilterHelper.Pitch(value));
        enabledFilters.Add($"Pitch: {value}");
        var newEmbed = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, player.Volume, enabledFilters);
        await ModifyNowPlayingMessage(newEmbed, null);
        return await EmbedHelper.MakeFilter(user, new [] {$"HANGMAGASSÁG -> {value}"});
    }

    public async Task<Embed> ClearFiltersAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
            return await EmbedHelper.MakeError(user, "Jelenleg nincs zene lejátszás alatt!");
        
        await player.ApplyFiltersAsync(FilterHelper.DefaultFilters());
        enabledFilters.Clear();
        var newEmbed = await EmbedHelper.MakeNowPlaying((SocketUser)ctx.User, player, isloopEnabled, player.Volume, enabledFilters);
        await ModifyNowPlayingMessage(newEmbed, null);
        return await EmbedHelper.MakeFilter(user, new [] {"MINDEN"});
    }
    
    public async Task<Embed> GetQueue(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null) return await EmbedHelper.MakeQueue(user, null, true);

        return await EmbedHelper.MakeQueue(user, player);
    }

    public async Task<Embed> ClearQueue(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null) 
            return await EmbedHelper.MakeError(user, "A lejátszó nem található!");
        player.Queue.Clear();
        return await EmbedHelper.MakeQueue(user, player, true);
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (!ShouldPlayNext(args.Reason)) return;
        previousTracks.Add(args.Track);
        var player = args.Player;
        if (!player.Queue.TryDequeue(out var queueable))
        {
            if (isloopEnabled) await player.PlayAsync(args.Track);
            // delete now playing message if there is no queue
            var msg = nowPlayingMessage ?? await ctx.Interaction.GetOriginalResponseAsync();
            await msg.DeleteAsync();
            await _lavaNode.LeaveAsync(player.VoiceChannel);
            nowPlayingMessage = null;
            previousTracks.Clear();
            return;
        }

        if (queueable is not { } track)
            //await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
            return;
        await player.PlayAsync(track);

        var newEmbed = await EmbedHelper.MakeNowPlaying(_client.CurrentUser, player, isloopEnabled, player.Volume, enabledFilters);
        var newComponents =
            await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), isPlaying);
        await ModifyNowPlayingMessage(newEmbed, newComponents);
    }

    private static bool ShouldPlayNext(TrackEndReason trackEndReason)
    {
        return trackEndReason is TrackEndReason.Finished or TrackEndReason.LoadFailed;
    }
    private bool CanGoBack()
    {
        return previousTracks.Count > 0;
    }
    
    private static bool CanGoForward(LavaPlayer player)
    {
        return player.Queue.Count > 0;
    }
    
    private static bool IsPlaying(LavaPlayer player)
    {
        return player.Track is not null && player.PlayerState is PlayerState.Playing ||
               player.PlayerState is PlayerState.Paused;
    }
    
    private async Task ModifyNowPlayingMessage(Embed embed, MessageComponent components)
    {
        var msg = nowPlayingMessage ?? await ctx.Interaction.GetOriginalResponseAsync();

        if (embed is not null && components is not null)
        {
            await msg.ModifyAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
            });
            nowPlayingMessage = msg;
        }
        else if (embed is not null)
        {
            await msg.ModifyAsync(x => x.Embed = embed);
            nowPlayingMessage = msg;
        }
        else if (components is not null)
        {
            await msg.ModifyAsync(x => x.Components = components);
            nowPlayingMessage = msg;
        }
    }

}