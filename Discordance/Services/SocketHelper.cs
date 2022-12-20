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
    private readonly ConcurrentDictionary<ulong, List<WebSocket>> _sockets = new();

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
            case ClientMessageType.SetController:
            {
                if (!_sockets.ContainsKey(message.GuildId))
                    _sockets.TryAdd(message.GuildId, new List<WebSocket>());
                _sockets[message.GuildId].Add(socket);
                await SendMessageAsync(message.GuildId, ServerMessageType.UpdateAll,
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
            if (player?.State is not PlayerState.Playing)
                continue;
            await SendMessageAsync(guildId, ServerMessageType.UpdatePosition, player)
                .ConfigureAwait(false);
            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }

    public async Task SendMessageAsync(ulong guildId, ServerMessageType type, MusicPlayer player)
    {
        if (!_sockets.ContainsKey(guildId))
            return;
        if (_sockets[guildId].Count == 0)
            return;
        _sockets[guildId].RemoveAll(s => s.State != WebSocketState.Open);
        var sockets = _sockets[guildId].ToList();
        foreach (var socket in sockets)
        {
            if (type.HasFlag(ServerMessageType.UpdateAll))
            {
                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new BaseServerMessage
                    {
                        Type = ServerMessageType.UpdateAll,
                        Payload = UpdateAllMessage.FromMusicPlayer(player)
                    }), WebSocketMessageType.Text, true,
                    CancellationToken.None).ConfigureAwait(false);
                continue;
            }

            if (type.HasFlag(ServerMessageType.UpdateQueue))
                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new BaseServerMessage
                    {
                        Type = ServerMessageType.UpdateQueue,
                        Payload = UpdateQueueMessage.FromQueue(player.Queue)
                    }), WebSocketMessageType.Text, true,
                    CancellationToken.None).ConfigureAwait(false);

            if (type.HasFlag(ServerMessageType.UpdatePosition))
                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new BaseServerMessage
                    {
                        Type = ServerMessageType.UpdatePosition,
                        Payload = new UpdatePositionMessage {Position = (int) player.Position.Position.TotalSeconds}
                    }), WebSocketMessageType.Text, true,
                    CancellationToken.None).ConfigureAwait(false);

            if (type.HasFlag(ServerMessageType.UpdatePlayerStatus))
                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new BaseServerMessage
                    {
                        Type = ServerMessageType.UpdatePlayerStatus,
                        Payload = UpdatePlayerStatusMessage.FromPlayer(player)
                    }), WebSocketMessageType.Text, true,
                    CancellationToken.None).ConfigureAwait(false);

            if (type.HasFlag(ServerMessageType.UpdateCurrentTrack))
                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new BaseServerMessage
                    {
                        Type = ServerMessageType.UpdateCurrentTrack,
                        Payload = UpdateCurrentTrackMessage.FromLavalinkTrack(player.CurrentTrack)
                    }), WebSocketMessageType.Text, true,
                    CancellationToken.None).ConfigureAwait(false);
        }
    }
}