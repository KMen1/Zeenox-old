using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discordance.Enums;
using Discordance.Models;
using Discordance.Models.Socket.Client;
using Discordance.Models.Socket.Server;
using Discordance.Modules.Music;
using Lavalink4NET.Player;

namespace Discordance.Services;

public sealed class SocketHelper
{
    private readonly AudioService _audioService;
    private readonly DiscordShardedClient _client;
    private readonly SearchService _searchService;
    private readonly ConcurrentDictionary<ulong, List<(ulong userId, WebSocket socket)>> _sockets = new();

    public SocketHelper(AudioService audioService, SearchService searchService, DiscordShardedClient client)
    {
        _searchService = searchService;
        _client = client;
        _audioService = audioService;
    }

    public async Task HandleClientMessage(BaseClientMessage message, WebSocket socket, CancellationToken token)
    {
        var user = _client.GetUser(message.UserId);
        switch (message.Type)
        {
            case ClientMessageType.PlayQuery:
            {
                var player = _audioService.GetPlayer(message.GuildId);
                if (player is null)
                    return;

                var tracks = await _searchService.SearchAsync(((PlayQueryMessage) message.Payload).Query, user,
                        AudioService.SearchMode.None)
                    .ConfigureAwait(false);
                if (tracks.Length > 1)
                    await _audioService.PlayAsync(message.GuildId, user, tracks).ConfigureAwait(false);

                await _audioService.PlayAsync(message.GuildId, user, tracks[0]).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.PlayQueueIndex:
            {
                var player = _audioService.GetPlayer(message.GuildId);
                if (player is null)
                    return;
                var index = ((PlayQueueIndexMessage) message.Payload).Index;
                var queueItem = player.Queue[index];
                player.Queue.RemoveAt(index);
                queueItem.Context = (TrackContext) queueItem.Context! with
                {
                    CoverUrl = await _searchService.GetCoverUrl(queueItem).ConfigureAwait(false)
                };
                await _audioService.PlayAsync(message.GuildId, _client.GetUser(message.UserId), queueItem, true)
                    .ConfigureAwait(false);
                break;
            }
            case ClientMessageType.Pause:
            {
                await _audioService.PauseOrResumeAsync(message.GuildId, user).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.Stop:
            {
                break;
            }
            case ClientMessageType.Skip:
            {
                await _audioService.SkipAsync(message.GuildId, user).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.Back:
            {
                await _audioService.RewindAsync(message.GuildId, user).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.SeekPosition:
            {
                break;
            }
            case ClientMessageType.SetVolume:
            {
                await _audioService.SetVolumeAsync(message.GuildId, user, ((SetVolumeMessage) message.Payload).Volume)
                    .ConfigureAwait(false);
                break;
            }
            case ClientMessageType.FavoriteTrack:
            {
                await _audioService.FavoriteTrackAsync(user, ((FavoriteTrackMessage) message.Payload).Id)
                    .ConfigureAwait(false);
                await SendMessageAsync(ServerMessageType.UpdateFavorites, _audioService.GetPlayer(message.GuildId)!)
                    .ConfigureAwait(false);
                break;
            }
            case ClientMessageType.RemoveTrackFromQueue:
            {
                var player = _audioService.GetPlayer(message.GuildId);
                if (player is null)
                    return;

                var index = ((RemoveTrackFromQueueMessage) message.Payload).Index;
                await player.RemoveFromQueue(index).ConfigureAwait(false);
                await SendMessageAsync(ServerMessageType.UpdateQueue, player).ConfigureAwait(false);
                break;
            }
            case ClientMessageType.SetController:
            {
                if (!_sockets.ContainsKey(message.GuildId))
                    _sockets.TryAdd(message.GuildId, new List<(ulong, WebSocket)>());
                if (_sockets[message.GuildId].Exists(x => x.userId == message.UserId))
                {
                    _sockets[message.GuildId].RemoveAll(x => x.userId == message.UserId);
                    _sockets[message.GuildId].Add((message.UserId, socket));
                }
                else
                {
                    _sockets[message.GuildId].Add((message.UserId, socket));
                }

                await SendMessageAsync(ServerMessageType.UpdateAll,
                    _audioService.GetPlayer(message.GuildId)!).ConfigureAwait(false);
                Task.Run(() => SendPositionLoopAsync(message.GuildId, token));
                break;
            }
        }
    }

    private async Task SendPositionLoopAsync(ulong guildId, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var player = _audioService.GetPlayer(guildId);
            if (player?.State is not PlayerState.Playing || player.CurrentTrack is null)
                continue;
            await SendMessageAsync(ServerMessageType.UpdatePosition, player)
                .ConfigureAwait(false);
            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }

    public async Task SendMessageAsync(ServerMessageType type, MusicPlayer player,
        UpdateQueueMessageType queueMessageType = UpdateQueueMessageType.Replace)
    {
        var guildId = player.GuildId;
        if (!_sockets.ContainsKey(guildId))
            return;
        if (_sockets[guildId].Count == 0)
            return;
        _sockets[guildId].RemoveAll(s => s.socket.State != WebSocketState.Open);
        var sockets = _sockets[guildId].ToList();
        var messages = CreateMessages(player, type, queueMessageType);
        var messagesJson = JsonSerializer.SerializeToUtf8Bytes(messages);

        foreach (var (_, socket) in sockets)
            await socket.SendAsync(messagesJson, WebSocketMessageType.Text, true, CancellationToken.None)
                .ConfigureAwait(false);
    }

    private static IEnumerable<BaseServerMessage> CreateMessages(MusicPlayer player, ServerMessageType type,
        UpdateQueueMessageType queueMessageType)
    {
        if (type.HasFlag(ServerMessageType.UpdateAll))
            yield return new BaseServerMessage
            {
                Type = ServerMessageType.UpdateAll,
                Payload = UpdateAllMessage.FromMusicPlayer(player)
            };

        if (type.HasFlag(ServerMessageType.UpdateFavorites))
            yield return new BaseServerMessage
            {
                Type = ServerMessageType.UpdateFavorites
            };
        
        if (type.HasFlag(ServerMessageType.UpdateQueue))
            yield return new BaseServerMessage
            {
                Type = ServerMessageType.UpdateQueue,
                Payload = UpdateQueueMessage.FromQueue(player.Queue, queueMessageType)
            };

        if (type.HasFlag(ServerMessageType.UpdatePosition))
            yield return new BaseServerMessage
            {
                Type = ServerMessageType.UpdatePosition,
                Payload = UpdatePositionMessage.FromSeconds(player.Position.Position)
            };

        if (type.HasFlag(ServerMessageType.UpdatePlayerStatus))
            yield return new BaseServerMessage
            {
                Type = ServerMessageType.UpdatePlayerStatus,
                Payload = UpdatePlayerStatusMessage.FromPlayer(player)
            };

        if (type.HasFlag(ServerMessageType.UpdateCurrentTrack))
            yield return new BaseServerMessage
            {
                Type = ServerMessageType.UpdateCurrentTrack,
                Payload = UpdateCurrentTrackMessage.FromLavalinkTrack(player.CurrentTrack,
                    player.LengthWithSponsorBlock)
            };
    }
}