using Lavalink4NET.Player;

namespace Zeenox.Models.Events;

public class PlayEventArgs : AudioEventArgs
{
    public PlayEventArgs(ulong guildId, LavalinkTrack track) : base(guildId)
    {
        Track = track;
    }

    public LavalinkTrack Track { get; }
}