using System;
using Lavalink4NET.Player;

namespace Zeenox.Models.Events;

public class TrackEndedEventArgs : EventArgs
{
    public TrackEndedEventArgs(ulong guildId, LavalinkTrack track, LavalinkTrack[] queue)
    {
        GuildId = guildId;
        Track = track;
        Queue = queue;
    }

    public ulong GuildId { get; }
    public LavalinkTrack Track { get; }
    public LavalinkTrack[] Queue { get; }
}