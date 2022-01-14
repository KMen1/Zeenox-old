using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
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

    private readonly List<LavaTrack> previousTracks = new();
    private string currentFilter;
    private bool isLooped;
    private IUserMessage nowPlayingMessage;
    private IDiscordInteraction currentInteraction;
    

    public AudioService(DiscordSocketClient client, LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
        _client = client;
    }

    public void InitializeAsync()
    {
        _client.Ready += OnReadyAsync;
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        _lavaNode.OnTrackEnded += OnTrackEnded;
        _lavaNode.OnTrackException += OnTrackException;
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.Id == _client.CurrentUser.Id && after.VoiceChannel == null && currentInteraction != null)
        {
            await DisconnectAsync(before.VoiceChannel.Guild);
            var msg = await GetNowPlayingMessage();
            if (msg != null)
            {
                await msg.Channel.SendMessageAsync(embed: new EmbedBuilder().WithColor(Color.Red)
                        .WithDescription("Egy barom lecsatlakoztatott a hangcsatornából ezért nem tudom folytatni a kívánt muzsika lejátszását. \n" +
                                         "Szánalmas vagy bárki is legyél, ha egyszer találkoznánk a torkodon nyomnám le azt az ujjadat amivel rákattintottál a lecsatlakoztatásra!")
                        .Build());
                await msg.DeleteAsync();
                ResetPlayer();
            }
        }
    }

    private async Task OnReadyAsync()
    {
        await _lavaNode.ConnectAsync();
    }

    private async Task OnTrackException(TrackExceptionEventArgs arg)
    {
        ResetPlayer();
        await arg.Player.StopAsync();
        await arg.Player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>());
        await arg.Player.TextChannel.SendMessageAsync(embed: await EmbedHelper.MakeError(arg.Exception.Message));
        await _lavaNode.LeaveAsync(arg.Player.VoiceChannel);
    }

    public async Task<Embed> DisconnectAsync(IGuild guild)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHelper.MakeError("Ezen a szerveren nem található lejátszó!");
        }
        var voiceChannel = _lavaNode.GetPlayer(guild).VoiceChannel;
        await _lavaNode.LeaveAsync(voiceChannel);
        return await EmbedHelper.MakeLeave(voiceChannel);
    }

    public async Task<Embed> MoveAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHelper.MakeError("Ezen a szerveren nem található lejátszó!");
        }
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        if (voiceChannel is null)
        {
            return await EmbedHelper.MakeError("Nem vagy hangcsatornában!");
        }
        await _lavaNode.MoveChannelAsync(voiceChannel);
        return await EmbedHelper.MakeMove(voiceChannel);
    }
    public async Task PlayAsync(string query, IGuild guild, ITextChannel tChannel, SocketUser user, 
        IDiscordInteraction interaction)
    {
        var voiceChannel = ((IVoiceState)user).VoiceChannel;
        if (voiceChannel is null)
        {
            await interaction.FollowupAsync(embed: await EmbedHelper.MakeError("Nem vagy hangcsatornában!"), ephemeral: true);
        }
        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query)
            : await _lavaNode.SearchYouTubeAsync(query);
        if (search.Status == SearchStatus.NoMatches)
        {
            await interaction.FollowupAsync(embed: await EmbedHelper.MakeError("Nincs találat!"), ephemeral: true);
        }
        var track = search.Tracks.FirstOrDefault();
        
        var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(voiceChannel, tChannel);
        
        if (IsPlaying(player))
        {
            player.Queue.Enqueue(track);
            await UpdateNowPlayingMessage(player, true, true);
            var msg = await interaction.FollowupAsync(embed: await EmbedHelper.MakeAddedToQueue(track, player), ephemeral: true);
            await Task.Delay(5000);
            await msg.DeleteAsync();
            return;
        }

        currentInteraction ??= interaction;
        await player.PlayAsync(track);
        await player.UpdateVolumeAsync(100);
        await interaction.FollowupAsync(
            embed: await EmbedHelper.MakeNowPlaying(user, player, isLooped, currentFilter), 
            components: await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), player));
    }

    public async Task StopAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }
        await player.StopAsync();
        ResetPlayer();
    }

    public async Task PlayNextTrack(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null || player.Queue.Count == 0)
        {
            return;
        }
        previousTracks.Add(player.Track);
        await player.SkipAsync();
        await UpdateNowPlayingMessage(player, true, true);
    }

    public async Task PlayPreviousTrack(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        var prev = previousTracks.Last();
        await player.PlayAsync(prev);
        previousTracks.Remove(prev);
        await UpdateNowPlayingMessage(player, true, true);
    }

    public async Task PauseOrResumeAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }

        switch (player.PlayerState)
        {
            case PlayerState.Playing:
            {
                await player.PauseAsync();
                await UpdateNowPlayingMessage(player, UpdateComponents: true);
                break;
            }
            case PlayerState.Paused:
            {
                await player.ResumeAsync();
                await UpdateNowPlayingMessage(player, UpdateComponents: true);
                break;
            }
        }
    }

    public async Task SetVolumeAsync(IGuild guild, VoiceButtonType buttonType)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }
        var currentVolume = player.Volume;
        if (currentVolume == 0 && buttonType == VoiceButtonType.VolumeDown || currentVolume == 100 && buttonType == VoiceButtonType.VolumeUp)
        {
            return;
        }
        switch (buttonType)
        {
            case VoiceButtonType.VolumeUp:
            {
                await player.UpdateVolumeAsync((ushort)(currentVolume + 10));
                await UpdateNowPlayingMessage(player, true, true);
                break;
            } 
            case VoiceButtonType.VolumeDown:
            {
                await player.UpdateVolumeAsync((ushort)(currentVolume - 10)); 
                await UpdateNowPlayingMessage(player, true, true);
                break;
            }
        }
    }
    
    public async Task<Embed> SetVolumeAsync(ushort volume, IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return await EmbedHelper.MakeError("A lejátszó nem található!");
        }

        await player.UpdateVolumeAsync(volume);
        await UpdateNowPlayingMessage(player, true, true);
        return await EmbedHelper.MakeVolume(player);
    }

    public async Task SetFiltersAsync(IGuild guild, FilterType filterType)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }
        await player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>());
        switch (filterType)
        {
            case FilterType.Bassboost:
            {
                await player.EqualizerAsync(FilterHelper.BassBoost());
                break;
            }
            case FilterType.Pop:
            {
                await player.EqualizerAsync(FilterHelper.Pop());
                break;
            }
            case FilterType.Soft:
            {
                await player.EqualizerAsync(FilterHelper.Soft());
                break;
            }
            case FilterType.Treblebass:
            {
                await player.EqualizerAsync(FilterHelper.TrebleBass());
                break;
            }
            case FilterType.Nightcore:
            {
                await player.ApplyFilterAsync(FilterHelper.NightCore());
                break;
            }
            case FilterType.Eightd:
            {
                await player.ApplyFilterAsync(FilterHelper.EightD());
                break;
            }
            case FilterType.Vaporwave:
            {
                var (filter, eq) = FilterHelper.VaporWave();
                await player.ApplyFiltersAsync(filter, equalizerBands: eq);
                break;
            }
            case FilterType.Doubletime:
            {
                await player.ApplyFilterAsync(FilterHelper.Doubletime());
                break;
            }
            case FilterType.Slowmotion:
            {
                await player.ApplyFilterAsync(FilterHelper.Slowmotion());
                break;
            }
            case FilterType.Chipmunk:
            {
                await player.ApplyFilterAsync(FilterHelper.Chipmunk());
                break;
            }
            case FilterType.Darthvader:
            {
                await player.ApplyFilterAsync(FilterHelper.Darthvader());
                break;
            }
            case FilterType.Dance:
            {
                await player.ApplyFilterAsync(FilterHelper.Dance());
                break;
            }
            case FilterType.China:
            {
                await player.ApplyFilterAsync(FilterHelper.China());
                break;
            }
            case FilterType.Vibrate:
            {
                await player.ApplyFiltersAsync(FilterHelper.Vibrate());
                break;
            }
            case FilterType.Vibrato:
            {
                await player.ApplyFilterAsync(FilterHelper.Vibrato());
                break;
            }
            case FilterType.Tremolo:
            {
                await player.ApplyFilterAsync(FilterHelper.Tremolo());
                break;
            }
        }
        var filterName = filterType.ToString();
        currentFilter = filterName;
        await UpdateNowPlayingMessage(player, true);
    }
    
    public async Task SetRepeatAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        isLooped = !isLooped;
        await UpdateNowPlayingMessage(player, true, true);
    }

    public async Task ClearFiltersAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
        {
            return;
        }
        await player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>());
        currentFilter = null;
        await UpdateNowPlayingMessage(player, true);
    }
    
    public async Task<Embed> GetQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return await EmbedHelper.MakeError("A lejátszó nem található!");
        }
        return await EmbedHelper.MakeQueue(player);
    }

    public async Task<Embed> ClearQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return await EmbedHelper.MakeError("A lejátszó nem található!");
        }
        player.Queue.Clear();
        return await EmbedHelper.MakeQueue(player, true);
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (!ShouldPlayNext(args.Reason))
        {
            return;
        }
        var player = args.Player;
        if (!player.Queue.TryDequeue(out var queueable))
        {
            if (isLooped) await player.PlayAsync(args.Track);
            
            var msg = await GetNowPlayingMessage();
            await msg.DeleteAsync();
            await _lavaNode.LeaveAsync(player.VoiceChannel);
            ResetPlayer();
            return;
        }
        if (queueable is not LavaTrack nextTrack)
        {
            return;
        }
        
        await player.PlayAsync(nextTrack);
        previousTracks.Add(args.Track);
        await UpdateNowPlayingMessage(player, true, true);
    }

    private async Task UpdateNowPlayingMessage(LavaPlayer player, bool UpdateEmbed = false, bool UpdateComponents = false)
    {
        var msg = await GetNowPlayingMessage();
        var embed = await EmbedHelper.MakeNowPlaying((SocketUser)currentInteraction.User, player, isLooped, currentFilter);
        var components = await ComponentHelper.MakeNowPlayingComponents(CanGoBack(), CanGoForward(player), player);

        if (UpdateEmbed && UpdateComponents)
        {
            await msg.ModifyAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
            });
        }
        else if (UpdateEmbed)
        {
            await msg.ModifyAsync(x => x.Embed = embed);
        }
        else if (UpdateComponents)
        {
            await msg.ModifyAsync(x => x.Components = components);
        }
    }
    
    private async Task<IUserMessage> GetNowPlayingMessage()
    {
        return nowPlayingMessage ?? await currentInteraction.GetOriginalResponseAsync();
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
    
    private void ResetPlayer()
    {
        currentFilter = null;
        previousTracks.Clear();
        nowPlayingMessage = null;
        currentInteraction = null;
        isLooped = false;
    }

}