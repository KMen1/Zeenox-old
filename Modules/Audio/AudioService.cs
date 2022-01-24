using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;
using KBot.Helpers;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Filters;
using Victoria.Responses.Search;

// ReSharper disable InconsistentNaming

namespace KBot.Modules.Audio;

public class AudioService
{
    private readonly DiscordSocketClient _client;
    private readonly LavaNode _lavaNode;
    private readonly DatabaseService _database;

    private Dictionary<ulong, bool> LoopEnabled { get; } = new();
    private Dictionary<ulong, string> FilterEnabled { get; } = new();
    private Dictionary<ulong, IUserMessage> NowPlayingMessage { get; } = new();
    private Dictionary<ulong, List<LavaTrack>> QueueHistory { get; } = new();

    public AudioService(DiscordSocketClient client, LavaNode lavaNode, DatabaseService database)
    {
        _lavaNode = lavaNode;
        _client = client;
        _database = database;
    }

    public void Initialize()
    {
        _client.Ready += OnReadyAsync;
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        _lavaNode.OnTrackEnded += OnTrackEndedAsync;
        _lavaNode.OnTrackException += OnTrackExceptionAsync;
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        var guild = before.VoiceChannel?.Guild ?? after.VoiceChannel?.Guild;
        if (guild == null)
        {
            return;
        }
        var nowPlayingMessage = NowPlayingMessage.GetValueOrDefault(guild.Id);
        if (user.Id == _client.CurrentUser.Id && after.VoiceChannel is null && nowPlayingMessage is not null)
        {
            await DisconnectAsync(before.VoiceChannel?.Guild).ConfigureAwait(false);
            await nowPlayingMessage.Channel.SendMessageAsync(embed: new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("Egy barom lecsatlakoztatott a hangcsatornából ezért nem tudom folytatni a kívánt muzsika lejátszását. \n" +
                                 "Szánalmas vagy bárki is legyél, ha egyszer találkoznánk a torkodon nyomnám le azt az ujjadat amivel rákattintottál a lecsatlakoztatásra!")
                .Build()).ConfigureAwait(false);
            await nowPlayingMessage.DeleteAsync().ConfigureAwait(false);
        }
    }

    private Task OnReadyAsync()
    {
        return _lavaNode.ConnectAsync();
    }

    private async Task OnTrackExceptionAsync(TrackExceptionEventArgs arg)
    {
        await arg.Player.StopAsync().ConfigureAwait(false);
        await arg.Player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>()).ConfigureAwait(false);
        await arg.Player.TextChannel.SendMessageAsync(embed: await EmbedHelper.ErrorEmbed(arg.Exception.Message).ConfigureAwait(false)).ConfigureAwait(false);
        await _lavaNode.LeaveAsync(arg.Player.VoiceChannel).ConfigureAwait(false);
    }

    public async Task<Embed> DisconnectAsync(IGuild guild)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHelper.ErrorEmbed("Ezen a szerveren nem található lejátszó!").ConfigureAwait(false);
        }
        var voiceChannel = _lavaNode.GetPlayer(guild).VoiceChannel;
        await _lavaNode.LeaveAsync(voiceChannel).ConfigureAwait(false);
        return await EmbedHelper.LeaveEmbed(voiceChannel).ConfigureAwait(false);
    }

    public async Task<Embed> MoveAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHelper.ErrorEmbed("Ezen a szerveren nem található lejátszó!").ConfigureAwait(false);
        }
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        if (voiceChannel is null)
        {
            return await EmbedHelper.ErrorEmbed("Nem vagy hangcsatornában!").ConfigureAwait(false);
        }
        await _lavaNode.MoveChannelAsync(voiceChannel).ConfigureAwait(false);
        return await EmbedHelper.MoveEmbed(voiceChannel).ConfigureAwait(false);
    }
    public async Task PlayAsync(string query, IGuild guild, ITextChannel tChannel, SocketUser user,
        IDiscordInteraction interaction)
    {
        var voiceChannel = ((IVoiceState)user).VoiceChannel;
        if (voiceChannel is null)
        {
            await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed("Nem vagy hangcsatornában!").ConfigureAwait(false), ephemeral: true).ConfigureAwait(false);
            return;
        }
        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query).ConfigureAwait(false) :
            await _lavaNode.SearchYouTubeAsync(query).ConfigureAwait(false);
        if (search.Status == SearchStatus.NoMatches)
        {
            await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed("Nincs találat!").ConfigureAwait(false), ephemeral: true).ConfigureAwait(false);
            return;
        }

        //var track = search.Tracks.FirstOrDefault();

        var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(voiceChannel, tChannel).ConfigureAwait(false);
        if (IsPlaying(player))
        {
            if (!string.IsNullOrWhiteSpace(search.Playlist.Name))
            {
                foreach (var track in search.Tracks)
                {
                    player.Queue.Enqueue(track);
                }
            }
            else
            {
                player.Queue.Enqueue(search.Tracks.FirstOrDefault());
            }
            await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
            var msg = await interaction
                .FollowupAsync(embed: await EmbedHelper.AddedToQueueEmbed(search.Tracks.FirstOrDefault(), player).ConfigureAwait(false),
                    ephemeral: true).ConfigureAwait(false);
            await Task.Delay(5000).ConfigureAwait(false);
            await msg.DeleteAsync().ConfigureAwait(false);
            return;
        }

        var message = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        NowPlayingMessage.Add(guild.Id, message);
        if (!string.IsNullOrWhiteSpace(search.Playlist.Name))
        {
            for (var i = 0; i < search.Tracks.Count; i++) {
                if (i == 0) {
                    await player.PlayAsync(search.Tracks.FirstOrDefault()).ConfigureAwait(false);
                }
                else {
                    player.Queue.Enqueue(search.Tracks.ElementAt(i));
                }
            }
        }
        else
        {
            await player.PlayAsync(search.Tracks.FirstOrDefault()).ConfigureAwait(false);
        }
        await player.UpdateVolumeAsync(100).ConfigureAwait(false);
        await interaction.FollowupAsync(
            embed: await EmbedHelper.NowPlayingEmbed(user, player, LoopEnabled.GetValueOrDefault(guild.Id), FilterEnabled.GetValueOrDefault(guild.Id)).ConfigureAwait(false),
            components: await ComponentHelper.NowPlayingComponents(await CanGoBack(guild.Id).ConfigureAwait(false), CanGoForward(player), player).ConfigureAwait(false)).ConfigureAwait(false);
    }

    public async Task StopAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }
        await player.StopAsync().ConfigureAwait(false);
    }

    public async Task PlayNextTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null || player.Queue.Count == 0)
        {
            return;
        }

        var queuehistory = QueueHistory.GetValueOrDefault(guild.Id);
        if (queuehistory is null)
        {
            queuehistory = new List<LavaTrack> {player.Track};
            QueueHistory[guild.Id] = queuehistory;
        }
        else
        {
            queuehistory.Add(player.Track);
            QueueHistory[guild.Id] = queuehistory;
        }
        await player.SkipAsync().ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
    }

    public async Task PlayPreviousTrackAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        var prev = QueueHistory.GetValueOrDefault(guild.Id)?.LastOrDefault();
        if (prev is null)
        {
            return;
        }
        await player.PlayAsync(prev).ConfigureAwait(false);
        QueueHistory[guild.Id].Remove(prev);
        await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
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
                await player.PauseAsync().ConfigureAwait(false);
                await UpdateNowPlayingMessageAsync(guild.Id, player, UpdateComponents: true).ConfigureAwait(false);
                break;
            }
            case PlayerState.Paused:
            {
                await player.ResumeAsync().ConfigureAwait(false);
                await UpdateNowPlayingMessageAsync(guild.Id, player, UpdateComponents: true).ConfigureAwait(false);
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
        if ((currentVolume == 0 && buttonType == VoiceButtonType.VolumeDown) || (currentVolume == 100 && buttonType == VoiceButtonType.VolumeUp))
        {
            return;
        }
        switch (buttonType)
        {
            case VoiceButtonType.VolumeUp:
            {
                await player.UpdateVolumeAsync((ushort)(currentVolume + 10)).ConfigureAwait(false);
                await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
                break;
            }
            case VoiceButtonType.VolumeDown:
            {
                await player.UpdateVolumeAsync((ushort)(currentVolume - 10)).ConfigureAwait(false); 
                await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
                break;
            }
        }
    }

    public async Task<Embed> SetVolumeAsync(ushort volume, IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return await EmbedHelper.ErrorEmbed("A lejátszó nem található!").ConfigureAwait(false);
        }

        await player.UpdateVolumeAsync(volume).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
        return await EmbedHelper.VolumeEmbed(player).ConfigureAwait(false);
    }

    public async Task SetFiltersAsync(IGuild guild, FilterType filterType)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }
        await player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>()).ConfigureAwait(false);
        switch (filterType)
        {
            case FilterType.Bassboost:
            {
                await player.EqualizerAsync(FilterHelper.BassBoost()).ConfigureAwait(false);
                break;
            }
            case FilterType.Pop:
            {
                await player.EqualizerAsync(FilterHelper.Pop()).ConfigureAwait(false);
                break;
            }
            case FilterType.Soft:
            {
                await player.EqualizerAsync(FilterHelper.Soft()).ConfigureAwait(false);
                break;
            }
            case FilterType.Treblebass:
            {
                await player.EqualizerAsync(FilterHelper.TrebleBass()).ConfigureAwait(false);
                break;
            }
            case FilterType.Nightcore:
            {
                await player.ApplyFilterAsync(FilterHelper.NightCore()).ConfigureAwait(false);
                break;
            }
            case FilterType.Eightd:
            {
                await player.ApplyFilterAsync(FilterHelper.EightD()).ConfigureAwait(false);
                break;
            }
            case FilterType.Vaporwave:
            {
                var (filter, eq) = FilterHelper.VaporWave();
                await player.ApplyFiltersAsync(filter, equalizerBands: eq).ConfigureAwait(false);
                break;
            }
            case FilterType.Doubletime:
            {
                await player.ApplyFilterAsync(FilterHelper.Doubletime()).ConfigureAwait(false);
                break;
            }
            case FilterType.Slowmotion:
            {
                await player.ApplyFilterAsync(FilterHelper.Slowmotion()).ConfigureAwait(false);
                break;
            }
            case FilterType.Chipmunk:
            {
                await player.ApplyFilterAsync(FilterHelper.Chipmunk()).ConfigureAwait(false);
                break;
            }
            case FilterType.Darthvader:
            {
                await player.ApplyFilterAsync(FilterHelper.Darthvader()).ConfigureAwait(false);
                break;
            }
            case FilterType.Dance:
            {
                await player.ApplyFilterAsync(FilterHelper.Dance()).ConfigureAwait(false);
                break;
            }
            case FilterType.China:
            {
                await player.ApplyFilterAsync(FilterHelper.China()).ConfigureAwait(false);
                break;
            }
            case FilterType.Vibrate:
            {
                await player.ApplyFiltersAsync(FilterHelper.Vibrate()).ConfigureAwait(false);
                break;
            }
            case FilterType.Vibrato:
            {
                await player.ApplyFilterAsync(FilterHelper.Vibrato()).ConfigureAwait(false);
                break;
            }
            case FilterType.Tremolo:
            {
                await player.ApplyFilterAsync(FilterHelper.Tremolo()).ConfigureAwait(false);
                break;
            }
        }
        var filterName = filterType.ToString();
        FilterEnabled[guild.Id] = filterName;
        await UpdateNowPlayingMessageAsync(guild.Id, player, true).ConfigureAwait(false);
    }

    public Task SetRepeatAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        LoopEnabled[guild.Id] = !LoopEnabled.GetValueOrDefault(guild.Id);
        return UpdateNowPlayingMessageAsync(guild.Id, player, true, true);
    }

    public async Task ClearFiltersAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
        {
            return;
        }
        await player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>()).ConfigureAwait(false);
        FilterEnabled[guild.Id] = null;
        await UpdateNowPlayingMessageAsync(guild.Id, player, true).ConfigureAwait(false);
    }

    public ValueTask<Embed> GetQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        return player is null ?
            EmbedHelper.ErrorEmbed("A lejátszó nem található!") : EmbedHelper.QueueEmbed(player);
    }

    public ValueTask<Embed> ClearQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return EmbedHelper.ErrorEmbed("A lejátszó nem található!");
        }
        player.Queue.Clear();
        return EmbedHelper.QueueEmbed(player, true);
    }

    private async Task OnTrackEndedAsync(TrackEndedEventArgs args)
    {
        if (!ShouldPlayNext(args.Reason))
        {
            return;
        }

        var guild = args.Player.TextChannel.Guild;
        var player = args.Player;
        if (!player.Queue.TryDequeue(out var queueable))
        {
            if (LoopEnabled.GetValueOrDefault(guild.Id))
            {
                await player.PlayAsync(args.Track).ConfigureAwait(false);
                return;
            }

            NowPlayingMessage.GetValueOrDefault(guild.Id)?.DeleteAsync().ConfigureAwait(false);
            await _lavaNode.LeaveAsync(player.VoiceChannel).ConfigureAwait(false);

            return;
        }
        if (queueable is not { } nextTrack)
        {
            return;
        }
        await player.PlayAsync(nextTrack).ConfigureAwait(false);
        var queuehistory = QueueHistory.GetValueOrDefault(guild.Id);
        if (queuehistory is null)
        {
            queuehistory = new List<LavaTrack> {args.Track};
            QueueHistory[guild.Id] = queuehistory;
        }
        else
        {
            queuehistory.Add(args.Track);
            QueueHistory[guild.Id] = queuehistory;
        }
        await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
    }

    private async Task UpdateNowPlayingMessageAsync(ulong guildId, LavaPlayer player, bool UpdateEmbed = false, bool UpdateComponents = false)
    {
        var message = NowPlayingMessage.GetValueOrDefault(guildId);
        if (message is null)
        {
            return;
        }
        var embed = await EmbedHelper.NowPlayingEmbed(_client.CurrentUser, player, LoopEnabled.GetValueOrDefault(guildId), FilterEnabled.GetValueOrDefault(guildId)).ConfigureAwait(false);
        var components = await ComponentHelper.NowPlayingComponents(await CanGoBack(guildId).ConfigureAwait(false), CanGoForward(player), player).ConfigureAwait(false);

        if (UpdateEmbed && UpdateComponents)
        {
            await message.ModifyAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
            }).ConfigureAwait(false);
        }
        else if (UpdateEmbed)
        {
            await message.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
        }
        else if (UpdateComponents)
        {
            await message.ModifyAsync(x => x.Components = components).ConfigureAwait(false);
        }
    }

    private static bool ShouldPlayNext(TrackEndReason trackEndReason)
    {
        return trackEndReason is TrackEndReason.Finished or TrackEndReason.LoadFailed;
    }
    private ValueTask<bool> CanGoBack(ulong guildId)
    {
        return new ValueTask<bool>(QueueHistory.GetValueOrDefault(guildId) is {Count: > 0});
    }
    private static bool CanGoForward(LavaPlayer player)
    {
        return player.Queue.Count > 0;
    }
    private static bool IsPlaying(LavaPlayer player)
    {
        return (player.Track is not null && player.PlayerState is PlayerState.Playing) ||
               player.PlayerState is PlayerState.Paused;
    }
}