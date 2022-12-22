using System;
using System.Collections.Generic;
using System.Linq;
using Lavalink4NET.Player;

namespace Discordance.Models.Socket.Server;

public struct UpdateQueueMessage : IServerMessage
{
    public UpdateQueueMessageType Type { get; init; }
    public int Count { get; init; }
    public TrackJson[] Tracks { get; init; }

    public static UpdateQueueMessage FromQueue(IEnumerable<LavalinkTrack> queue,
        UpdateQueueMessageType queueMessageType)
    {
        var lavalinkTracks = queue as LavalinkTrack[] ?? queue.ToArray();
        var tracks = queueMessageType switch
        {
            UpdateQueueMessageType.Add => lavalinkTracks.TakeLast(1).Select(track => TrackJson.FromLavalinkTrack(track))
                .ToArray(),
            UpdateQueueMessageType.Remove => Array.Empty<TrackJson>(),
            UpdateQueueMessageType.Replace => lavalinkTracks.Select((track, i) => TrackJson.FromLavalinkTrack(track, i))
                .ToArray(),
            UpdateQueueMessageType.Clear => Array.Empty<TrackJson>(),
            _ => throw new ArgumentOutOfRangeException(nameof(queueMessageType), queueMessageType, null)
        };
        return new UpdateQueueMessage
        {
            Type = queueMessageType,
            Count = lavalinkTracks.Length,
            Tracks = tracks
        };
    }
}

public enum UpdateQueueMessageType
{
    Add,
    Remove,
    Clear,
    Replace
}