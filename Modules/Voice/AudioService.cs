using System;
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

namespace KBot.Modules.Voice;

public class AudioService
{
    private readonly DiscordSocketClient _client;
    private readonly LavaNode _lavaNode;
    private readonly DatabaseService _database;

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
        var currentInteraction = await _database.GetNowPlayingMessageAsync(guild.Id).ConfigureAwait(false);
        if (user.Id == _client.CurrentUser.Id && after.VoiceChannel is null && currentInteraction.messageId is not 0)
        {
            await DisconnectAsync(before.VoiceChannel?.Guild).ConfigureAwait(false);
            var (channelId, messageId) = await _database.GetNowPlayingMessageAsync(before.VoiceChannel.Guild.Id).ConfigureAwait(false);

            var channel = await _client.GetChannelAsync(channelId).ConfigureAwait(false) as IMessageChannel;
            IUserMessage message = null;
            if (channel is not null)
            {
                message = await channel.GetMessageAsync(messageId).ConfigureAwait(false) as IUserMessage;
            }
            if (message != null)
            {
                await message.Channel.SendMessageAsync(embed: new EmbedBuilder().WithColor(Color.Red)
                        .WithDescription("Egy barom lecsatlakoztatott a hangcsatornából ezért nem tudom folytatni a kívánt muzsika lejátszását. \n" +
                                         "Szánalmas vagy bárki is legyél, ha egyszer találkoznánk a torkodon nyomnám le azt az ujjadat amivel rákattintottál a lecsatlakoztatásra!")
                        .Build()).ConfigureAwait(false);
                await message.DeleteAsync().ConfigureAwait(false);
                ResetPlayer();
            }
        }
    }

    private Task OnReadyAsync()
    {
        return _lavaNode.ConnectAsync();
    }

    private async Task OnTrackExceptionAsync(TrackExceptionEventArgs arg)
    {
        ResetPlayer();
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
        var track = search.Tracks.FirstOrDefault();

        var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(voiceChannel, tChannel).ConfigureAwait(false);
        if (IsPlaying(player))
        {
            player.Queue.Enqueue(track);
            await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
            var msg = await interaction
                .FollowupAsync(embed: await EmbedHelper.AddedToQueueEmbed(track, player).ConfigureAwait(false),
                    ephemeral: true).ConfigureAwait(false);
            await Task.Delay(5000).ConfigureAwait(false);
            await msg.DeleteAsync().ConfigureAwait(false);
            return;
        }

        var message = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        await _database.SetNowPlayingMessageAsync(guild.Id, message.Channel.Id, message.Id).ConfigureAwait(false);

        await player.PlayAsync(track).ConfigureAwait(false);
        await player.UpdateVolumeAsync(100).ConfigureAwait(false);
        var currentFilter = await _database.GetEnabledFilterAsync(guild.Id).ConfigureAwait(false);
        var isLooped = await _database.GetLoopEnabledAsync(guild.Id).ConfigureAwait(false);
        await interaction.FollowupAsync(
            embed: await EmbedHelper.NowPlayingEmbed(user, player, isLooped, currentFilter).ConfigureAwait(false),
            components: await ComponentHelper.NowPlayingComponents(await CanGoBackAsync(guild.Id).ConfigureAwait(false), CanGoForward(player), player).ConfigureAwait(false)).ConfigureAwait(false);
    }

    public async Task StopAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }
        await player.StopAsync().ConfigureAwait(false);
        ResetPlayer();
    }

    public async Task PlayNextTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null || player.Queue.Count == 0)
        {
            return;
        }
        await _database.AddTrackToHistoryAsync(guild.Id, user.Id, player.Track).ConfigureAwait(false);
        await player.SkipAsync().ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
    }

    public async Task PlayPreviousTrackAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        var prev = await _database.GetTrackFromHistoryAsync(guild.Id, true).ConfigureAwait(false);
        await player.PlayAsync(prev.Track).ConfigureAwait(false);
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
        await _database.SetEnabledFilterAsync(guild.Id, filterName).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, true).ConfigureAwait(false);
    }

    public async Task SetRepeatAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        var loop = await _database.GetLoopEnabledAsync(guild.Id).ConfigureAwait(false);
        await _database.SetLoopEnabledAsync(guild.Id, !loop).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
    }

    public async Task ClearFiltersAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is not {PlayerState: PlayerState.Playing})
        {
            return;
        }
        await player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>()).ConfigureAwait(false);
        await _database.SetEnabledFilterAsync(guild.Id, string.Empty).ConfigureAwait(false);
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
            if (await _database.GetLoopEnabledAsync(guild.Id).ConfigureAwait(false)) await player.PlayAsync(args.Track).ConfigureAwait(false);
            var (channelId, messageId) = await _database.GetNowPlayingMessageAsync(guild.Id).ConfigureAwait(false);
            var channel = await _client.GetChannelAsync(channelId).ConfigureAwait(false) as IMessageChannel;
            IMessage msg = null;
            if (channel is not null)
            {
                msg = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
            }
            if (msg is IUserMessage msgToDelete)
            {
                await msgToDelete.DeleteAsync().ConfigureAwait(false);
            }
            await _lavaNode.LeaveAsync(player.VoiceChannel).ConfigureAwait(false);
            ResetPlayer();
            return;
        }
        if (queueable is not { } nextTrack)
        {
            return;
        }
        await player.PlayAsync(nextTrack).ConfigureAwait(false);
        await _database.AddTrackToHistoryAsync(guild.Id, _client.CurrentUser.Id, args.Track).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, true, true).ConfigureAwait(false);
    }

    private async Task UpdateNowPlayingMessageAsync(ulong guildId, LavaPlayer player, bool UpdateEmbed = false, bool UpdateComponents = false)
    {
        var (channelId, messageId) = await _database.GetNowPlayingMessageAsync(guildId).ConfigureAwait(false);
        var channel = await _client.GetChannelAsync(channelId).ConfigureAwait(false) as IMessageChannel;
        IUserMessage message = null;
        if (channel is not null)
        {
            message = await channel.GetMessageAsync(messageId).ConfigureAwait(false) as IUserMessage;
        }
        if (message is null)
        {
            return;
        }
        var currentFilter = await _database.GetEnabledFilterAsync(guildId).ConfigureAwait(false);
        var isLooped = await _database.GetLoopEnabledAsync(guildId).ConfigureAwait(false);
        var embed = await EmbedHelper.NowPlayingEmbed(_client.CurrentUser, player, isLooped, currentFilter).ConfigureAwait(false);
        var components = await ComponentHelper.NowPlayingComponents(await CanGoBackAsync(guildId).ConfigureAwait(false), CanGoForward(player), player).ConfigureAwait(false);

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
    private async Task<bool> CanGoBackAsync(ulong guildId)
    {
        var track = await _database.GetTrackFromHistoryAsync(guildId, false).ConfigureAwait(false);
        return track is not null;
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
    private void ResetPlayer()
    {
        //previousTracks.Clear();
    }
}