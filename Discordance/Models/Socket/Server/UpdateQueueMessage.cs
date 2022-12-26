using System;
using System.Collections.Generic;
using System.Linq;
using Discordance.Extensions;
using Lavalink4NET.Player;

namespace Discordance.Models.Socket.Server;

public struct UpdateQueueMessage : IServerMessage
{
    public UpdateQueueMessageType Type { get; init; }
    public int Count { get; init; }
    public TrackData[] Tracks { get; init; }
    public string DurationString { get; init; }

    public static UpdateQueueMessage FromQueue(IEnumerable<LavalinkTrack> queue,
        UpdateQueueMessageType queueMessageType)
    {
        var lavalinkTracks = queue as LavalinkTrack[] ?? queue.ToArray();
        var tracks = queueMessageType switch
        {
            UpdateQueueMessageType.Add => lavalinkTracks.TakeLast(1).Select(track => TrackData.FromLavalinkTrack(track))
                .ToArray(),
            UpdateQueueMessageType.Remove => Array.Empty<TrackData>(),
            UpdateQueueMessageType.Replace => lavalinkTracks.Select((track, i) => TrackData.FromLavalinkTrack(track, i))
                .ToArray(),
            UpdateQueueMessageType.Clear => Array.Empty<TrackData>(),
            _ => throw new ArgumentOutOfRangeException(nameof(queueMessageType), queueMessageType, null)
        };
        var duration = lavalinkTracks.Sum(track => track.Duration.TotalMilliseconds);
        var length = TimeSpan.FromMilliseconds(duration);
        var durationString = length.ToTimeString();
        return new UpdateQueueMessage
        {
            Type = queueMessageType,
            Count = lavalinkTracks.Length,
            Tracks = tracks,
            DurationString = durationString
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