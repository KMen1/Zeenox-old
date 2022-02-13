using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using KBot.Modules.Audio.Enums;
using KBot.Modules.Audio.Helpers;
using Microsoft.Extensions.Logging;
using Serilog;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Filters;
using Victoria.Responses.Search;

namespace KBot.Modules.Audio;

public class AudioService : DiscordClientService
{
    private readonly DiscordSocketClient _client;
    private readonly LavaNode _lavaNode;

    private Dictionary<ulong, bool> LoopEnabled { get; } = new();
    private Dictionary<ulong, string> FilterEnabled { get; } = new();
    private Dictionary<ulong, IUserMessage> NowPlayingMessage { get; } = new();
    private Dictionary<ulong, LinkedList<(LavaTrack track, SocketUser user)>> Queue { get; } = new();
    private Dictionary<ulong, List<LavaTrack>> QueueHistory { get; } = new();
    private Dictionary<ulong, SocketUser> LastRequestedBy { get; } = new();

    private readonly Dictionary<FilterType, EqualizerBand[]> EqualizerBands = new()
    {
        {FilterType.Bassboost, Helpers.Filters.BassBoost()},
        {FilterType.Pop, Helpers.Filters.Pop()},
        {FilterType.Soft, Helpers.Filters.Soft()},
        {FilterType.Treblebass, Helpers.Filters.TrebleBass()},
    };

    private readonly Dictionary<FilterType, IFilter> Filters = new()
    {
        {FilterType.Nightcore, Helpers.Filters.NightCore()},
        {FilterType.Eightd, Helpers.Filters.EightD()},
        {FilterType.Vaporwave, Helpers.Filters.VaporWave()},
        {FilterType.Doubletime, Helpers.Filters.Doubletime()},
        {FilterType.Slowmotion, Helpers.Filters.Slowmotion()},
        {FilterType.Chipmunk, Helpers.Filters.Chipmunk()},
        {FilterType.Darthvader, Helpers.Filters.Darthvader()},
        {FilterType.Dance, Helpers.Filters.Dance()},
        {FilterType.China, Helpers.Filters.China()},
        //{FilterType.Vibrate, Helpers.Filters.Vibrate()},
        {FilterType.Vibrato, Helpers.Filters.Vibrato()},
        {FilterType.Tremolo, Helpers.Filters.Tremolo()},
    };

    public AudioService(DiscordSocketClient client, ILogger<AudioService> logger, LavaNode lavaNode) : base(client, logger)
    {
        _lavaNode = lavaNode;
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        _lavaNode.OnTrackEnded += OnTrackEndedAsync;
        _lavaNode.OnTrackException += OnTrackExceptionAsync;
        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        await _lavaNode.ConnectAsync().ConfigureAwait(false);
        Log.Logger.Information("Audio Service Loaded");
    }
    
    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        var guild = before.VoiceChannel?.Guild ?? after.VoiceChannel?.Guild;

        var nowPlayingMessage = NowPlayingMessage.GetValueOrDefault(guild!.Id);
        if (user.Id == _client.CurrentUser.Id && after.VoiceChannel is null && nowPlayingMessage is not null)
        {
            var log = guild.GetAuditLogsAsync(1, actionType: ActionType.MemberDisconnected).Flatten();
            var logUser = await log.FirstAsync().ConfigureAwait(false);
            var guildUser = guild.GetUser(logUser.User.Id);
            await guildUser.ModifyAsync(x => x.Channel = null).ConfigureAwait(false);
            await DisconnectAsync(before.VoiceChannel?.Guild).ConfigureAwait(false);
            await nowPlayingMessage.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription($"{guildUser.Mention} lecsatlakoztatott a hangcsatornából ezért nem tudom folytatni a kívánt muzsika lejátszását. \n" +
                                 "Milyen érzés ha téged is lecsatlakoztatnak? :kissing_heart: ")
                .Build()).ConfigureAwait(false);
            await nowPlayingMessage.DeleteAsync().ConfigureAwait(false);
        }
    }

    private async Task OnTrackExceptionAsync(TrackExceptionEventArgs arg)
    {
        await arg.Player.TextChannel.SendMessageAsync(embed: Embeds.ErrorEmbed(arg.Exception.Message)).ConfigureAwait(false);
        await _lavaNode.LeaveAsync(arg.Player.VoiceChannel).ConfigureAwait(false);
    }

    public async Task<Embed> DisconnectAsync(IGuild guild)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return Embeds.ErrorEmbed("Ezen a szerveren nem található lejátszó!");
        }
        var voiceChannel = _lavaNode.GetPlayer(guild).VoiceChannel;
        ResetPlayer(guild.Id);
        await _lavaNode.LeaveAsync(voiceChannel).ConfigureAwait(false);
        return Embeds.LeaveEmbed(voiceChannel);
    }

    public async Task<Embed> MoveAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild))
        {
            return Embeds.ErrorEmbed("Ezen a szerveren nem található lejátszó!");
        }
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        if (voiceChannel is null)
        {
            return Embeds.ErrorEmbed("Nem vagy hangcsatornában!");
        }
        await _lavaNode.MoveChannelAsync(voiceChannel).ConfigureAwait(false);
        return Embeds.MoveEmbed(voiceChannel);
    }
    public async Task PlayAsync(IGuild guild, SocketInteraction interaction, string query)
    {
        var user = interaction.User;
        var textChannel = (ITextChannel)interaction.Channel;
        var voiceChannel = ((IVoiceState)user).VoiceChannel;
        var searchResponse = await SearchAsync(query).ConfigureAwait(false);
        if (searchResponse.Tracks is null)
        {
            await interaction.FollowupAsync(embed: Embeds.ErrorEmbed("Nincs találat!")).ConfigureAwait(false);
            return;
        }

        var queue = Queue.GetValueOrDefault(guild.Id) ?? new LinkedList<(LavaTrack, SocketUser)>();
        var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(voiceChannel, textChannel).ConfigureAwait(false);

        if (IsPlaying(player))
        {
            if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
            {
                foreach (var track in searchResponse.Tracks)
                {
                    queue.AddLast((track, user));
                }
                Queue[guild.Id] = queue;
                var msg = await interaction
                    .FollowupAsync(embed: Embeds.AddedToQueueEmbed(searchResponse.Tracks.ToList())).ConfigureAwait(false);
                await Task.Delay(5000).ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            }
            else
            {
                queue.AddLast((searchResponse.Tracks.First(), user));
                Queue[guild.Id] = queue;
                var msg = await interaction.FollowupAsync(embed: Embeds.AddedToQueueEmbed(new List<LavaTrack>{searchResponse.Tracks.First()})).ConfigureAwait(false);
                await Task.Delay(5000).ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            }
            await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true, true).ConfigureAwait(false);
            return;
        }

        NowPlayingMessage[guild.Id] = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
        {
            foreach (var track in searchResponse.Tracks.Skip(1))
            {
                queue.AddLast((track, user));
            }
            Queue[guild.Id] = queue;
        }
        await player.PlayAsync(searchResponse.Tracks.First()).ConfigureAwait(false);
        LastRequestedBy[guild.Id] = user;
        await player.UpdateVolumeAsync(100).ConfigureAwait(false);
        await interaction.FollowupAsync(
            embed: await Embeds.NowPlayingEmbed(user, player, LoopEnabled.GetValueOrDefault(guild.Id), FilterEnabled.GetValueOrDefault(guild.Id), queue.Count).ConfigureAwait(false),
            components: Components.NowPlayingComponents(CanGoBack(guild.Id), CanGoForward(guild.Id), player)).ConfigureAwait(false);
    }
    public async Task PlayAsync(IGuild guild, SocketInteraction interaction, LavaTrack track)
    {
        var user = interaction.User;
        var textChannel = (ITextChannel)interaction.Channel;
        var voiceChannel = ((IVoiceState)user).VoiceChannel;
        if (voiceChannel is null)
        {
            await interaction.FollowupAsync(embed: Embeds.ErrorEmbed("Nem vagy hangcsatornában!")).ConfigureAwait(false);
            return;
        }

        var queue = Queue.GetValueOrDefault(guild.Id) ?? new LinkedList<(LavaTrack, SocketUser)>();
        var player = _lavaNode.HasPlayer(guild) ? _lavaNode.GetPlayer(guild) : await _lavaNode.JoinAsync(voiceChannel, textChannel).ConfigureAwait(false);
        if (IsPlaying(player))
        {
            queue.AddLast((track, user));
            Queue[guild.Id] = queue;
            var msg = await interaction.FollowupAsync(embed: Embeds.AddedToQueueEmbed(new List<LavaTrack>{track})).ConfigureAwait(false);
            await Task.Delay(5000).ConfigureAwait(false);
            await msg.DeleteAsync().ConfigureAwait(false);
            var choose = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);
            await choose.DeleteAsync().ConfigureAwait(false);
            await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true, true).ConfigureAwait(false);
            return;
        }

        await player.PlayAsync(track).ConfigureAwait(false);
        await player.UpdateVolumeAsync(100).ConfigureAwait(false);
        var msgt = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        var embed = await Embeds.NowPlayingEmbed(user, player, LoopEnabled.GetValueOrDefault(guild.Id),
            FilterEnabled.GetValueOrDefault(guild.Id), queue.Count).ConfigureAwait(false);
        await msgt.ModifyAsync(x =>
        {
            x.Embed = embed;
            x.Components = Components.NowPlayingComponents(CanGoBack(guild.Id), CanGoForward(guild.Id), player);
        }).ConfigureAwait(false);
        NowPlayingMessage[guild.Id] = msgt;
        LastRequestedBy[guild.Id] = user;
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

    public async Task PlayNextTrackAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        var nextTrack = Queue.GetValueOrDefault(guild.Id)?.First?.Value;
        if (player is null || nextTrack is null)
        {
            return;
        }

        var queuehistory = QueueHistory.GetValueOrDefault(guild.Id) ?? new List<LavaTrack>();
        queuehistory.Add(player.Track);
        QueueHistory[guild.Id] = queuehistory;
        Queue.GetValueOrDefault(guild.Id)?.RemoveFirst();
        LastRequestedBy[guild.Id] = nextTrack.Value.user;
        await player.PlayAsync(nextTrack.Value.track).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, nextTrack.Value.user, true, true).ConfigureAwait(false);
    }

    public async Task PlayPreviousTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer(guild);
        var prev = QueueHistory.GetValueOrDefault(guild.Id)?.LastOrDefault();
        if (prev is null)
        {
            await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], updateComponents: true).ConfigureAwait(false);
            return;
        }
        await player.PlayAsync(prev).ConfigureAwait(false);
        QueueHistory[guild.Id].Remove(prev);
        LastRequestedBy[guild.Id] = user;
        await UpdateNowPlayingMessageAsync(guild.Id, player, user, true, true).ConfigureAwait(false);
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
                await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], updateComponents: true).ConfigureAwait(false);
                break;
            }
            case PlayerState.Paused:
            {
                await player.ResumeAsync().ConfigureAwait(false);
                await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], updateComponents: true).ConfigureAwait(false);
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
                await player.UpdateVolumeAsync((ushort)(currentVolume + 10)).ConfigureAwait(false);
                await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true, true).ConfigureAwait(false);
                break;
            }
            case VoiceButtonType.VolumeDown:
            {
                await player.UpdateVolumeAsync((ushort)(currentVolume - 10)).ConfigureAwait(false); 
                await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true, true).ConfigureAwait(false);
                break;
            }
        }
    }

    public async Task<Embed> SetVolumeAsync(IGuild guild, ushort volume)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return Embeds.ErrorEmbed("A lejátszó nem található!");
        }

        await player.UpdateVolumeAsync(volume).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true, true).ConfigureAwait(false);
        return Embeds.VolumeEmbed(player);
    }

    public async Task SetFiltersAsync(IGuild guild, FilterType filterType)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return;
        }
        await player.ApplyFiltersAsync(Array.Empty<IFilter>(), equalizerBands: Array.Empty<EqualizerBand>()).ConfigureAwait(false);
        if (Filters.ContainsKey(filterType))
        {
            await player.ApplyFilterAsync(Filters[filterType]).ConfigureAwait(false);
        }
        else if (EqualizerBands.ContainsKey(filterType))
        {
            await player.EqualizerAsync(EqualizerBands[filterType]).ConfigureAwait(false);
        }
        FilterEnabled[guild.Id] = filterType.ToString();
        await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true).ConfigureAwait(false);
    }

    public Task SetRepeatAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        LoopEnabled[guild.Id] = !LoopEnabled.GetValueOrDefault(guild.Id);
        return UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true, true);
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
        await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true).ConfigureAwait(false);
    }

    public Embed GetQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        return player is null ?
            Embeds.ErrorEmbed("A lejátszó nem található!") :
            Embeds.QueueEmbed(player, Queue.GetValueOrDefault(guild.Id));
    }

    public async Task<Embed> ClearQueueAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer(guild);
        if (player is null)
        {
            return Embeds.ErrorEmbed("A lejátszó nem található!");
        }
        Queue.GetValueOrDefault(guild.Id)?.Clear();
        await UpdateNowPlayingMessageAsync(guild.Id, player, LastRequestedBy[guild.Id], true).ConfigureAwait(false);
        return Embeds.QueueEmbed(player, null, true);
    }

    private async Task OnTrackEndedAsync(TrackEndedEventArgs args)
    {
        if (!ShouldPlayNext(args.Reason))
        {
            return;
        }
        var guild = args.Player.TextChannel.Guild;
        var player = args.Player;

        if (LoopEnabled.GetValueOrDefault(guild.Id))
        {
            await player.PlayAsync(args.Track).ConfigureAwait(false);
            return;
        }
        var nextTrack = Queue.GetValueOrDefault(args.Player.TextChannel.GuildId)?.First?.Value;
        if (nextTrack is not null)
        {
            Queue.GetValueOrDefault(args.Player.TextChannel.GuildId)?.RemoveFirst();
            await args.Player.PlayAsync(nextTrack.Value.track).ConfigureAwait(false);

            var queuehistory = QueueHistory.GetValueOrDefault(guild.Id) ?? new List<LavaTrack>();
            queuehistory.Add(args.Track);
            QueueHistory[guild.Id] = queuehistory;
            LastRequestedBy[guild.Id] = nextTrack.Value.user;
            await UpdateNowPlayingMessageAsync(guild.Id, player, nextTrack.Value.user,true, true).ConfigureAwait(false);
            return;
        }
        NowPlayingMessage.GetValueOrDefault(guild.Id)?.DeleteAsync().ConfigureAwait(false);
        await _lavaNode.LeaveAsync(player.VoiceChannel).ConfigureAwait(false);
        ResetPlayer(guild.Id);
    }

    private async Task UpdateNowPlayingMessageAsync(ulong guildId, LavaPlayer player, IUser user,
        bool updateEmbed = false, bool updateComponents = false)
    {
        var message = NowPlayingMessage.GetValueOrDefault(guildId);
        if (message is null)
        {
            return;
        }

        user ??= message.Interaction.User;
        var queueLength = Queue.GetValueOrDefault(guildId) is null ? 0 : Queue[guildId].Count;
        var embed = await Embeds.NowPlayingEmbed(user, player, LoopEnabled.GetValueOrDefault(guildId),
            FilterEnabled.GetValueOrDefault(guildId), queueLength).ConfigureAwait(false);
        var components = Components.NowPlayingComponents(CanGoBack(guildId), CanGoForward(guildId), player);

        if (updateEmbed && updateComponents)
        {
            await message.ModifyAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
            }).ConfigureAwait(false);
        }
        else if (updateEmbed)
        {
            await message.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
        }
        else if (updateComponents)
        {
            await message.ModifyAsync(x => x.Components = components).ConfigureAwait(false);
        }
    }

    private static bool ShouldPlayNext(TrackEndReason trackEndReason)
    {
        return trackEndReason is TrackEndReason.Finished or TrackEndReason.LoadFailed;
    }
    private bool CanGoBack(ulong guildId)
    {
        return QueueHistory.GetValueOrDefault(guildId) is {Count: > 0};
    }
    private bool CanGoForward(ulong guildId)
    {
        return Queue.GetValueOrDefault(guildId) is {Count: > 0};
    }
    private static bool IsPlaying(LavaPlayer player)
    {
        return (player.Track is not null && player.PlayerState is PlayerState.Playing) ||
               player.PlayerState is PlayerState.Paused;
    }

    private void ResetPlayer(ulong guildId)
    {
        NowPlayingMessage[guildId] = null;
        FilterEnabled[guildId] = null;
        QueueHistory[guildId] = null;
        Queue[guildId] = null;
        LastRequestedBy[guildId] = null;
    }

    public async Task<SearchResponse> SearchAsync(string query)
    {
        var results = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
            await _lavaNode.SearchAsync(SearchType.Direct, query).ConfigureAwait(false) :
            await _lavaNode.SearchYouTubeAsync(query).ConfigureAwait(false);
        return results.Status is not SearchStatus.NoMatches or SearchStatus.LoadFailed ? results : new SearchResponse();
    }
}