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
using Lavalink4NET.Filters;
using Lavalink4NET.Payloads.Player;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;

namespace KBot.Modules.Music;

public class AudioService : IInjectable
{
    private readonly Dictionary<FilterType, Func<PlayerFilterMap, string>> _filterActions = new()
    {
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

    public AudioService(DiscordSocketClient client, LavalinkNode lavaNode, YouTubeService youtubeService)
    {
        _lavaNode = lavaNode;
        _youtubeService = youtubeService;
        client.Ready += async () => await _lavaNode.InitializeAsync().ConfigureAwait(false);
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

        await player.NowPlayingMessage.DeleteAsync().ConfigureAwait(false);
        player.NowPlayingMessage = null;
        await player!.DisconnectAsync().ConfigureAwait(false);
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

    public async Task PlayAsync(IGuild guild, SocketInteraction interaction, string query)
    {
        var user = interaction.User;
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        var searchResponse = await SearchAsync(query).ConfigureAwait(false);
        if (searchResponse is null)
        {
            await interaction
                .FollowupAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithTitle("No matches!").Build())
                .ConfigureAwait(false);
            return;
        }

        var users = await voiceChannel.GetUsersAsync().FlattenAsync().ConfigureAwait(false);
        var skipVotesNeeded = users.Count(x => !x.IsBot) / 2;
        var player = _lavaNode.HasPlayer(guild.Id)
            ? _lavaNode.GetPlayer<MusicPlayer>(guild.Id)
            : await _lavaNode
                .JoinAsync(() => new MusicPlayer(voiceChannel, skipVotesNeeded, _youtubeService, _lavaNode), guild.Id,
                    voiceChannel.Id).ConfigureAwait(false);
        var isPlaylist = searchResponse.PlaylistInfo?.Name is not null;

        if (player!.IsPlaying)
        {
            if (isPlaylist)
            {
                foreach (var track in searchResponse.Tracks!) track.Context = new TrackContext(user);
                player.Enqueue(searchResponse.Tracks);
                _ = Task.Run(async () =>
                {
                    var msg = await interaction.FollowupAsync(embed:
                        new EmbedBuilder().AddedToQueueEmbed(searchResponse.Tracks)).ConfigureAwait(false);
                    await Task.Delay(1750).ConfigureAwait(false);
                    await msg.DeleteAsync().ConfigureAwait(false);
                });
                await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
                return;
            }

            searchResponse.Tracks![0].Context = new TrackContext(user);
            player.Enqueue(searchResponse.Tracks![0]);
            _ = Task.Run(async () =>
            {
                var msg = await interaction.FollowupAsync(embed:
                        new EmbedBuilder().AddedToQueueEmbed(new List<LavalinkTrack> {searchResponse.Tracks[0]}))
                    .ConfigureAwait(false);
                await Task.Delay(1750).ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            });
            await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
            return;
        }

        if (isPlaylist)
        {
            foreach (var track in searchResponse.Tracks!) track.Context = new TrackContext(user);
            await player.PlayAsync(searchResponse.Tracks![0]).ConfigureAwait(false);
            player.Enqueue(searchResponse.Tracks.Skip(1));
        }
        else
        {
            searchResponse.Tracks![0].Context = new TrackContext(user);
            await player.PlayAsync(searchResponse.Tracks![0]).ConfigureAwait(false);
        }

        player.NowPlayingMessage = await interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        await interaction.FollowupAsync(
            embed: new EmbedBuilder().NowPlayingEmbed(player),
            components: new ComponentBuilder().NowPlayerComponents(player)).ConfigureAwait(false);
    }

    public async Task PlayFromSearchAsync(IGuild guild, SocketMessageComponent interaction, string id)
    {
        var user = interaction.User;
        var voiceChannel = ((IVoiceState) user).VoiceChannel;
        var track = await _lavaNode.GetTrackAsync("https://www.youtube.com/watch?v=" + id).ConfigureAwait(false);
        track!.Context = new TrackContext(user);
        var skipVotesNeeded = await voiceChannel.GetUsersAsync().CountAsync().ConfigureAwait(false) / 2;
        var player = _lavaNode.HasPlayer(guild.Id)
            ? _lavaNode.GetPlayer<MusicPlayer>(guild.Id)
            : await _lavaNode
                .JoinAsync(() => new MusicPlayer(voiceChannel, skipVotesNeeded, _youtubeService, _lavaNode), guild.Id,
                    voiceChannel.Id).ConfigureAwait(false);
        var interactionMessage = interaction.Message;

        if (player!.IsPlaying)
        {
            player!.Enqueue(track);
            await interactionMessage.DeleteAsync().ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                var msg = await interaction
                    .FollowupAsync(embed: new EmbedBuilder().AddedToQueueEmbed(new List<LavalinkTrack> {track}))
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
            x.Embed = new EmbedBuilder().NowPlayingEmbed(player);
            x.Components = new ComponentBuilder().NowPlayerComponents(player);
        }).ConfigureAwait(false);
        player.NowPlayingMessage = interactionMessage;
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
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public async Task PlayPreviousTrackAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null || player.LastRequestedBy.Id != user.Id) return;
        await player!.PlayPreviousAsync().ConfigureAwait(false);
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public async Task<Embed> PauseOrResumeAsync(IGuild guild, SocketUser user)
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

        return null;
    }

    public async Task<Embed> SetVolumeAsync(IGuild guild, SocketUser user, VoiceButtonType buttonType)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();
        var currentVolume = player.Volume;
        if (currentVolume == 0f && buttonType == VoiceButtonType.VolumeDown ||
            currentVolume == 1f && buttonType == VoiceButtonType.VolumeUp)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Volume must be between 0 and 100!").Build();
        switch (buttonType)
        {
            case VoiceButtonType.VolumeUp:
            {
                await player.SetVolumeAsync(currentVolume + 10 / 100f).ConfigureAwait(false);
                await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
                break;
            }
            case VoiceButtonType.VolumeDown:
            {
                await player.SetVolumeAsync(currentVolume - 10 / 100f).ConfigureAwait(false);
                await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
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
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        return new EmbedBuilder().VolumeEmbed(player);
    }

    public async Task<Embed> SetFiltersAsync(IGuild guild, SocketUser user, FilterType filterType)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();
        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();
        var noFiltersPayload = new PlayerFiltersPayload(guild.Id, new Dictionary<string, IFilterOptions>());
        await _lavaNode.SendPayloadAsync(noFiltersPayload).ConfigureAwait(false);

        player.FilterEnabled = _filterActions[filterType](player.Filters);
        await player.Filters.CommitAsync().ConfigureAwait(false);
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        return null;
    }

    public Task SetRepeatAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        player!.Loop = !player.Loop;
        return player.UpdateNowPlayingMessageAsync();
    }

    public async Task<Embed> ClearFiltersAsync(IGuild guild, SocketUser user)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is not {State: PlayerState.Playing})
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        if (player.LastRequestedBy.Id != user.Id)
            return new EmbedBuilder().WithColor(Color.Red)
                .WithDescription("**Only the person who added the currently playing song can control the bot!**")
                .Build();

        var noFiltersPayload = new PlayerFiltersPayload(guild.Id, new Dictionary<string, IFilterOptions>());
        await _lavaNode.SendPayloadAsync(noFiltersPayload).ConfigureAwait(false);
        player.FilterEnabled = null;
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
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
        player.ClearQueue();
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        return new EmbedBuilder().QueueEmbed(player, true);
    }

    public async Task<TrackLoadResponsePayload> SearchAsync(string query)
    {
        var results = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.LoadTracksAsync(query).ConfigureAwait(false)
            : await _lavaNode.LoadTracksAsync(query!, SearchMode.YouTube).ConfigureAwait(false);
        return results.Tracks is not null ? results : null;
    }

    public async Task<Embed> ToggleAutoplayAsync(IGuild guild)
    {
        var player = _lavaNode.GetPlayer<MusicPlayer>(guild.Id);
        if (player is null)
            return new EmbedBuilder().WithColor(Color.Red).WithTitle("Not currently playing in this server!").Build();

        player.AutoPlay = !player.AutoPlay;
        await player.UpdateNowPlayingMessageAsync().ConfigureAwait(false);
        return new EmbedBuilder().WithColor(Color.Green)
            .WithTitle("Autoplay has been" + (player.AutoPlay ? "enabled!" : "disabled!")).Build();
    }
}