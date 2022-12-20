using System.Collections.Generic;
using System.Linq;
using Lavalink4NET.Player;

namespace Discordance.Models.Socket.Server;

public struct UpdateQueueMessage : IServerMessage
{
    public int Count { get; init; }
    public TrackJson[] Tracks { get; init; }

    public static UpdateQueueMessage FromQueue(IEnumerable<LavalinkTrack> queue)
    {
        var lavalinkTracks = queue as LavalinkTrack[] ?? queue.ToArray();
        return new UpdateQueueMessage
        {
            Count = lavalinkTracks.Length,
            Tracks = lavalinkTracks.Select((track, i) => TrackJson.FromLavalinkTrack(track, i)).ToArray()
        };
    }
}