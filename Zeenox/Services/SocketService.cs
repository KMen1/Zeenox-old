using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Lavalink4NET.Player;
using Zeenox.Enums;
using Zeenox.Models;
using Zeenox.Models.Events;
using Zeenox.Models.Socket.Client;
using Zeenox.Models.Socket.Server;
using Zeenox.Modules.Music;

namespace Zeenox.Services;

public sealed class SocketService
{
    private readonly AudioService _audioService;
    private readonly DiscordShardedClient _discord;
    private readonly MongoService _mongoService;
    private readonly SearchService _searchService;
    private readonly ConcurrentDictionary<ulong, List<(ulong userId, WebSocket socket)>> _sockets = new();

    public SocketService(AudioService audioService, MongoService mongoService, SearchService searchService,
        DiscordShardedClient discord)
    {
        _audioService = audioService;
        _audioService.OnPlayAsync += OnPlayAsync;
        _audioService.OnQueueChangedAsync += OnTrackQueuedAsync;
        _audioService.OnTrackEndedAsync += OnTrackEndedAsync;
        _audioService.OnPlayerStatusChangedAsync += OnPlayerStatusChangedAsync;
        _mongoService = mongoService;
        _searchService = searchService;
        _discord = discord;
    }

    private Task OnTrackEndedAsync(object sender, TrackEndedEventArgs eventargs)
    {
        return SendMessagesToAllAsync(new[]
        {
            new BaseServerMessage
            {
                Type = ServerMessageType.UpdateQueue,
                Payload = UpdateQueueMessage.FromQueue(eventargs.Queue, UpdateQueueMessageType.Replace)
            },
            new BaseServerMessage
            {
                Type = ServerMessageType.UpdateCurrentTrack,
                Payload = UpdateCurrentTrackMessage.FromLavalinkTrack(eventargs.Track)
            }
        }, eventargs.GuildId);
    }

    private Task OnPlayerStatusChangedAsync(object sender, PlayerStatusChangedEventArgs eventargs)
    {
        return SendMessageToAllAsync(new BaseServerMessage
        {
            Type = ServerMessageType.UpdatePlayerStatus,
            Payload = UpdatePlayerStatusMessage.FromPlayer((MusicPlayer) sender)
        }, eventargs.GuildId);
    }

    private Task OnTrackQueuedAsync(object sender, QueueChangedEventArgs eventargs)
    {
        var queue = eventargs.Tracks;
        return SendMessageToAllAsync(new BaseServerMessage
        {
            Type = ServerMessageType.UpdateQueue,
            Payload = UpdateQueueMessage.FromQueue(queue, UpdateQueueMessageType.Replace)
        }, eventargs.GuildId);
    }

    private Task OnPlayAsync(object sender, PlayEventArgs eventargs)
    {
        var track = eventargs.Track;
        return SendMessageToAllAsync(new BaseServerMessage
        {
            Type = ServerMessageType.UpdateCurrentTrack,
            Payload = UpdateCurrentTrackMessage.FromLavalinkTrack(track)
        }, eventargs.GuildId);
    }

    public async Task HandleClientMessage(BaseClientMessage message, WebSocket socket, CancellationToken token)
    {
        var guildId = message.GuildId;
        var userId = message.UserId;
        var user = _discord.GetUser(message.UserId);
        switch (message.Type)
        {
            case ClientMessageType.PlayQuery:
            {
                var player = _audioService.GetPlayer(guildId);
                if (player is null)
                    return;

                var tracks = await _searchService.SearchAsync(((PlayQueryMessage) message.Payload).Query, user,
                        SearchMode.None)
                    .ConfigureAwait(false);
                if (tracks.Length > 1)
                    await _audioService.PlayAsync(guildId, tracks).ConfigureAwait(false);

                await _audioService.PlayAsync(guildId, tracks[0]).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.PlayQueueIndex:
            {
                var player = _audioService.GetPlayer(guildId);
                if (player is null)
                    return;
                var index = ((PlayQueueIndexMessage) message.Payload).Index;
                var queueItem = player.Queue[index];
                player.Queue.RemoveAt(index);
                queueItem.Context = (TrackContext) queueItem.Context! with
                {
                    CoverUrl = await _searchService.GetCoverUrl(queueItem).ConfigureAwait(false)
                };
                await _audioService.PlayAsync(guildId, queueItem, true)
                    .ConfigureAwait(false);
                break;
            }
            case ClientMessageType.Pause:
            {
                await _audioService.PauseOrResumeAsync(guildId).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.Stop:
            {
                break;
            }
            case ClientMessageType.Skip:
            {
                await _audioService.SkipAsync(guildId, user).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.Back:
            {
                await _audioService.RewindAsync(guildId).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.SeekPosition:
            {
                await _audioService.SeekAsync(guildId, ((SeekPositionMessage) message.Payload).Position)
                    .ConfigureAwait(false);
                break;
            }
            case ClientMessageType.SetVolume:
            {
                await _audioService.SetVolumeAsync(guildId, ((SetVolumeMessage) message.Payload).Volume)
                    .ConfigureAwait(false);
                break;
            }
            case ClientMessageType.FavoriteTrack:
            {
                var dbUser = await _mongoService.GetUserAsync(userId).ConfigureAwait(false);
                var favorites = dbUser.Playlists[0].Songs;
                var id = ((FavoriteTrackMessage) message.Payload).Id;
                User newDbUser;
                if (favorites.Contains(id))
                    newDbUser = await _mongoService.UpdateUserAsync(userId, x => x.Playlists[0].Songs.Remove(id))
                        .ConfigureAwait(false);
                else
                    newDbUser = await _mongoService.UpdateUserAsync(userId, x => x.Playlists[0].Songs.Add(id))
                        .ConfigureAwait(false);

                await SendMessageToUserAsync(new BaseServerMessage
                    {
                        Type = ServerMessageType.UpdateFavorites,
                        Payload = UpdateFavoritesMessage.Create(newDbUser.Playlists[0].Songs)
                    }, userId)
                    .ConfigureAwait(false);
                break;
            }
            case ClientMessageType.RemoveTrackFromQueue:
            {
                var player = _audioService.GetPlayer(guildId);
                if (player is null)
                    return;

                var index = ((RemoveTrackFromQueueMessage) message.Payload).Index;
                await player.RemoveFromQueue(index).ConfigureAwait(false);
                await SendMessageToAllAsync(new BaseServerMessage
                {
                    Type = ServerMessageType.UpdateQueue,
                    Payload = UpdateQueueMessage.FromQueue(player.Queue, UpdateQueueMessageType.Replace)
                }, guildId).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.SetController:
            {
                if (!_sockets.ContainsKey(guildId))
                    _sockets.TryAdd(guildId, new List<(ulong, WebSocket)>());
                if (_sockets[guildId].Exists(x => x.userId == userId))
                {
                    _sockets[guildId].RemoveAll(x => x.userId == userId);
                    _sockets[guildId].Add((userId, socket));
                }
                else
                {
                    _sockets[message.GuildId].Add((userId, socket));
                }

                var favorites = (await _mongoService.GetUserAsync(userId).ConfigureAwait(false)).Playlists[0].Songs;
                await SendMessageToUserAsync(
                    new BaseServerMessage
                    {
                        Type = ServerMessageType.UpdateAll,
                        Payload = UpdateAllMessage.FromMusicPlayer(_audioService.GetPlayer(guildId)!, favorites)
                    }
                    , userId).ConfigureAwait(false);
                _ = Task.Run(() => SendPositionLoopAsync(guildId, token), token);
                break;
            }
            /*case ClientMessageType.GetPlaylists:
            {
                var dbUser = await _mongoService.GetUserAsync(userId).ConfigureAwait(false);
                var playlists = dbUser.Playlists;

                await SendMessageToUserAsync(guildId, userId, new BaseServerMessage
                {
                    Type = ServerMessageType.UpdatePlaylists,
                    Payload = PlaylistsMessage.FromPlaylists(playlists)
                }).ConfigureAwait(false);
                break;
            }*/
            case ClientMessageType.GetVoiceState:
            {
                await SendMessageToUserAsync(new BaseServerMessage
                {
                    Type = ServerMessageType.UpdateVoiceState,
                    Payload = UpdateVoiceStateMessage.GetVoiceStateMessage(_discord, guildId, userId)
                }, userId).ConfigureAwait(false);
                break;
            }
        }
    }

    private async Task SendPositionLoopAsync(ulong guildId, CancellationToken token)
    {
        var player = _audioService.GetPlayer(guildId);
        while (!token.IsCancellationRequested)
        {
            if (player?.State is PlayerState.Destroyed)
                player = _audioService.GetPlayer(guildId);
            if (player?.State is not PlayerState.Playing || player.CurrentTrack is null)
                continue;
            if (player.Position.Position > player.CurrentTrack.Duration)
                continue;
            await SendMessageToAllAsync(new BaseServerMessage
                {
                    Type = ServerMessageType.UpdatePosition,
                    Payload = UpdatePositionMessage.FromSeconds(player.Position.Position)
                }, guildId)
                .ConfigureAwait(false);
            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }

    private async Task SendMessageToAllAsync(BaseServerMessage message, ulong guildId)
    {
        RemoveClosedSockets();
        if (!ShouldSendToAll(guildId)) return;

        var sockets = GetSockets(guildId);

        foreach (var socket in sockets) await SendMessageAsync(message, socket).ConfigureAwait(false);
    }

    private async Task SendMessagesToAllAsync(BaseServerMessage[] messages, ulong guildId)
    {
        RemoveClosedSockets();
        if (!ShouldSendToAll(guildId)) return;

        var sockets = GetSockets(guildId);

        foreach (var socket in sockets) await SendMessagesAsync(messages, socket).ConfigureAwait(false);
    }

    private async Task SendMessageToUserAsync(BaseServerMessage message, ulong userId)
    {
        RemoveClosedSockets();
        if (!ShouldSendToUser(userId)) return;

        var socket = GetSocket(userId);
        if (socket is null)
            return;

        await SendMessageAsync(message, socket).ConfigureAwait(false);
    }

    private static Task SendMessageAsync(BaseServerMessage message, WebSocket socket)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new[] {message});
        return socket.SendAsync(bytes, WebSocketMessageType.Text, true, default);
    }

    private static Task SendMessagesAsync(BaseServerMessage[] messages, WebSocket socket)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(messages);
        return socket.SendAsync(bytes, WebSocketMessageType.Text, true, default);
    }

    private bool ShouldSendToAll(ulong guildId)
    {
        if (!_sockets.ContainsKey(guildId))
            return false;
        if (_sockets[guildId].Count == 0)
            return false;

        return true;
    }

    private bool ShouldSendToUser(ulong userId)
    {
        foreach (var (_, sockets) in _sockets)
            if (sockets.Any(x => x.userId == userId))
                return true;

        return false;
    }

    private IEnumerable<WebSocket> GetSockets(ulong guildId)
    {
        if (!_sockets.ContainsKey(guildId))
            return new List<WebSocket>();
        return _sockets[guildId].Select(x => x.socket);
    }

    private WebSocket? GetSocket(ulong userId)
    {
        foreach (var (_, sockets) in _sockets)
        {
            var socket = sockets.Find(x => x.userId == userId);
            if (socket.socket is not null)
                return socket.socket;
        }

        return null;
    }

    private void RemoveClosedSockets()
    {
        foreach (var (key, value) in _sockets)
        {
            foreach (var (userId, socket) in value)
                if (socket.State == WebSocketState.Closed)
                    value.Remove((userId, socket));

            if (value.Count == 0)
                _sockets.TryRemove(key, out _);
        }
    }
}