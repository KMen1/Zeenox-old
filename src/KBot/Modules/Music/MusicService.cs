using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Modules.Music.Helpers;
using Lavalink4NET;
using Lavalink4NET.Filters;
using Lavalink4NET.Logging;
using Lavalink4NET.Payloads.Player;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Serilog;
using ILogger = Lavalink4NET.Logging.ILogger;

namespace KBot.Modules.Music;

public class AudioService
{
    private readonly LavalinkNode _lavaNode;

    public AudioService(DiscordSocketClient client, LavalinkNode lavaNode, ILogger logger)
    {
        _lavaNode = lavaNode;
        client.Ready += async () => await _lavaNode.InitializeAsync().ConfigureAwait(false);
        var log = logger as EventLogger;
        log!.LogMessage += (_, args) => Log.Information(args.Message);
    }

    public async Task<Embed> DisconnectAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild.Id))
        {
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Ezen a szerveren nem található lejátszó!").Build();
        }
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player!.LastRequestedBy.Id != user.Id)
        {
            return null;
        }

        await player.NowPlayingMessage.DeleteAsync().ConfigureAwait(false);
        player.NowPlayingMessage = null;
        await player!.DisconnectAsync().ConfigureAwait(false);
        return new EmbedBuilder().LeaveEmbed(player.VoiceChannel);
    }

    public async Task<Embed> MoveAsync(IGuild guild, SocketUser user)
    {
        if (!_lavaNode.HasPlayer(guild.Id))
        {
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Ezen a szerveren nem található lejátszó!").Build();
        }
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        if (voiceChannel is null)
        {
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Nem vagy hangcsatornában").Build();
        }

        var player = _lavaNode.GetPlayer(guild.Id);
        await player!.ConnectAsync(voiceChannel.Id).ConfigureAwait(false);
        return new EmbedBuilder().MoveEmbed(voiceChannel);
    }
    public async Task PlayAsync(IGuild guild, SocketInteraction interaction, string query)
    {
        var user = interaction.User;
        var textChannel = (ITextChannel)interaction.Channel;
        var voiceChannel = ((IVoiceState)user).VoiceChannel;
        var searchResponse = await SearchAsync(query).ConfigureAwait(false);
        if (searchResponse is null)
        {
            await interaction.FollowupAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithTitle("Nincs találat! Kérlek próbáld újra másképp!").Build()).ConfigureAwait(false);
            return;
        }
        var player = _lavaNode.HasPlayer(guild.Id) ?
            _lavaNode.GetPlayer<MusicPlayer>(guild.Id) :
            await _lavaNode.JoinAsync(() => new MusicPlayer(voiceChannel, textChannel), guild.Id, voiceChannel.Id).ConfigureAwait(false);
        var queue = player!.Queue;

        var isPlaylist = searchResponse.PlaylistInfo?.Name is not null;
        var firstTrack = searchResponse.Tracks?[0];

        if (isPlaylist)
        {
            var tracks = IsPlaying(player) ? searchResponse.Tracks!.ToList() : searchResponse.Tracks!.Skip(1).ToList();
            foreach (var track in tracks)
            {
                track.Context = new MusicPlayer.TrackContext(user);
            }
            queue.AddRange(tracks);
        }
        firstTrack!.Context = new MusicPlayer.TrackContext(user);

        if (IsPlaying(player))
        {
            Embed qeueEmbed;
            if (isPlaylist)
            {
                qeueEmbed = new EmbedBuilder().AddedToQueueEmbed(searchResponse.Tracks.ToList());
            }
            else
            {
                qeueEmbed = new EmbedBuilder().AddedToQueueEmbed(new List<LavalinkTrack>(new[] { firstTrack }));
                queue.Add(firstTrack);
            }

            _ = Task.Run(async () =>
            {
                var msg = await interaction.FollowupAsync(embed: qeueEmbed).ConfigureAwait(false);
                await Task.Delay(1750).ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            });
            await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
            return;
        }
        player.NowPlayingMessage = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        await player!.PlayAsync(firstTrack).ConfigureAwait(false);
        await interaction.FollowupAsync(
            embed: new EmbedBuilder().NowPlayingEmbed(user, player),
            components: Components.NowPlayingComponents(player)).ConfigureAwait(false);
    }
    public async Task PlayAsync(IGuild guild, SocketInteraction interaction, LavalinkTrack track)
    {
        var user = interaction.User;
        var textChannel = (ITextChannel)interaction.Channel;
        var voiceChannel = ((IVoiceState)user).VoiceChannel;
        if (voiceChannel is null)
        {
            await interaction.FollowupAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithTitle("Nem vagy hangcsatornában").Build()).ConfigureAwait(false);
            return;
        }
        var player = _lavaNode.HasPlayer(guild.Id) ?
            _lavaNode.GetPlayer<MusicPlayer>(guild.Id) :
            await _lavaNode.JoinAsync(() => new MusicPlayer(voiceChannel, textChannel), guild.Id, voiceChannel.Id).ConfigureAwait(false);
        var interactionMessage = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        if (IsPlaying(player))
        {
            player!.Queue.Add(track);
            await interactionMessage.DeleteAsync().ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                var msg = await interaction
                    .FollowupAsync(embed: new EmbedBuilder().AddedToQueueEmbed(new List<LavalinkTrack>(new[] {track})))
                    .ConfigureAwait(false);
                await Task.Delay(1750).ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            });
            
            await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
            return;
        }

        await player!.PlayAsync(track).ConfigureAwait(false);
        await interactionMessage.ModifyAsync(x =>
        {
            x.Embed = new EmbedBuilder().NowPlayingEmbed(user, player);
            x.Components = Components.NowPlayingComponents(player);
        }).ConfigureAwait(false);
        player.NowPlayingMessage = interactionMessage;
    }

    public async Task StopAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
        {
            return;
        }
        if (player.LastRequestedBy.Id != user.Id)
        {
            return;
        }
        await player.StopAsync().ConfigureAwait(false);
    }

    public async Task PlayNextTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        var nextTrack = player?.Queue.FirstOrDefault();
        if (player is null || nextTrack is null)
        {
            return;
        }
        if (player.LastRequestedBy.Id != user.Id)
        {
            return;
        }
        var queuehistory = player.QueueHistory;
        queuehistory.Add(player.CurrentTrack);
        player.Queue.RemoveAt(0);
        await player.PlayAsync(nextTrack).ConfigureAwait(false);
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public async Task PlayPreviousTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        var prev = player?.QueueHistory.LastOrDefault();
        if (prev is null)
        {
            await player!.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
            return;
        }
        if (player.LastRequestedBy.Id != user.Id)
        {
            return;
        }
        await player!.PlayAsync(prev).ConfigureAwait(false);
        player.QueueHistory.Remove(prev);
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public async Task PauseOrResumeAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
        {
            return;
        }
        if (player.LastRequestedBy.Id != user.Id)
        {
            return;
        }
        switch (player.State)
        {
            case PlayerState.Playing:
            {
                await player.PauseAsync().ConfigureAwait(false);
                await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
                break;
            }
            case PlayerState.Paused:
            {
                await player.ResumeAsync().ConfigureAwait(false);
                await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
                break;
            }
        }
    }

    public async Task SetVolumeAsync(IGuild guild, SocketUser user, VoiceButtonType buttonType)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
        {
            return;
        }
        if (player.LastRequestedBy.Id != user.Id)
        {
            return;
        }
        var currentVolume = player.Volume;
        if (currentVolume == 0f && buttonType == VoiceButtonType.VolumeDown || currentVolume == 1f && buttonType == VoiceButtonType.VolumeUp)
        {
            return;
        }
        switch (buttonType)
        {
            case VoiceButtonType.VolumeUp:
            {
                await player.SetVolumeAsync(currentVolume + 10/100f).ConfigureAwait(false);
                await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
                break;
            }
            case VoiceButtonType.VolumeDown:
            {
                await player.SetVolumeAsync(currentVolume - 10/100f).ConfigureAwait(false); 
                await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
                break;
            }
        }
    }

    public async Task<Embed> SetVolumeAsync(IGuild guild, ushort volume)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
        {
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Ezen a szerveren nem található lejátszó!").Build();
        }
        await player.SetVolumeAsync(volume).ConfigureAwait(false);
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        return new EmbedBuilder().VolumeEmbed(player);
    }

    public async Task SetFiltersAsync(IGuild guild, SocketUser user, FilterType filterType)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
        {
            return;
        }
        if (player.LastRequestedBy.Id != user.Id)
        {
            return;
        }
        PlayerFiltersPayload t = new PlayerFiltersPayload(guild.Id, new Dictionary<string, IFilterOptions>());
        await _lavaNode.SendPayloadAsync(t).ConfigureAwait(false);
        player.Filters.Equalizer = new EqualizerFilterOptions();
        switch (filterType)
        {
            case FilterType.Bassboost:
            {
                player.Filters.Equalizer.Bands = Filters.BassBoost();
                player.FilterEnabled = "Basszus Erősítés";
                break;
            }
            case FilterType.Pop:
            {
                player.Filters.Equalizer.Bands = Filters.Pop();
                player.FilterEnabled = "Pop";
                break;
            }
            case FilterType.Soft:
            {
                player.Filters.Equalizer.Bands = Filters.Soft();
                player.FilterEnabled = "Lágy";
                break;
            }
            case FilterType.Treblebass:
            {
                player.Filters.Equalizer.Bands = Filters.TrebleBass();
                player.FilterEnabled = "Hangos";
                break;
            }
            case FilterType.Nightcore:
            {
                player.Filters.Timescale = Filters.NightCore();
                player.FilterEnabled = "Nightcore";
                break;
            }
            case FilterType.Eightd:
            {
                player.Filters.Rotation = Filters.EightD();
                player.FilterEnabled = "8D";
                break;
            }
            case FilterType.Vaporwave:
            {
                player.Filters.Timescale = Filters.VaporWave();
                player.FilterEnabled = "Vaporwave";
                break;
            }
            case FilterType.Doubletime:
            {
                player.Filters.Timescale = Filters.Doubletime();
                player.FilterEnabled = "Gyorsítás";
                break;
            }
            case FilterType.Slowmotion:
            {
                player.Filters.Timescale = Filters.Slowmotion();
                player.FilterEnabled = "Lassítás";
                break;
            }
            case FilterType.Chipmunk:
            {
                player.Filters.Timescale = Filters.Chipmunk();
                player.FilterEnabled = "Alvin és a mókusok";
                break;
            }
            case FilterType.Darthvader:
            {
                player.Filters.Timescale = Filters.Darthvader();
                player.FilterEnabled = "Darth Vader";
                break;
            }
            case FilterType.Dance:
            {
                player.Filters.Timescale = Filters.Dance();
                player.FilterEnabled = "Tánc";
                break;
            }
            case FilterType.China:
            {
                player.Filters.Timescale = Filters.China();
                player.FilterEnabled = "Kínai";
                break;
            }
            case FilterType.Vibrato:
            {
                player.Filters.Vibrato = Filters.Vibrato();
                player.FilterEnabled = "Vibrato";
                break;
            }
            case FilterType.Tremolo:
            {
                player.Filters.Tremolo = Filters.Tremolo();
                player.FilterEnabled = "Tremolo";
                break;
            }
        }

        await player.Filters.CommitAsync().ConfigureAwait(false);
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public Task SetRepeatAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        player!.LoopEnabled = !player.LoopEnabled;
        return player.UpdateNowPlayingMessageAsync();
    }

    public async Task ClearFiltersAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is not {State: PlayerState.Playing})
        {
            return;
        }
        if (player.LastRequestedBy.Id != user.Id)
        {
            return;
        }

        PlayerFiltersPayload t = new PlayerFiltersPayload(guild.Id, new Dictionary<string, IFilterOptions>());
        await _lavaNode.SendPayloadAsync(t).ConfigureAwait(false);
        player.FilterEnabled = null;
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public Embed GetQueue(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        return player is null ?
            new EmbedBuilder().WithColor(Color.Red).WithTitle("Ezen a szerveren nem található lejátszó!").Build() :
            new EmbedBuilder().QueueEmbed(player);
    }

    public async Task<Embed> ClearQueueAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
        {
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Ezen a szerveren nem található lejátszó!").Build();
        }
        player.Queue.Clear();
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        return new EmbedBuilder().QueueEmbed(player, true);
    }

    private static bool IsPlaying(LavalinkPlayer player)
    {
        return (player.CurrentTrack is not null && player.State is PlayerState.Playing) ||
               player.State is PlayerState.Paused;
    }

    public async Task<TrackLoadResponsePayload> SearchAsync(string query)
    {
        var results = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
            await _lavaNode.LoadTracksAsync(query).ConfigureAwait(false) :
            await _lavaNode.LoadTracksAsync(query!, SearchMode.YouTube).ConfigureAwait(false);
        return results.Tracks is not null ? results : null;
    }
}