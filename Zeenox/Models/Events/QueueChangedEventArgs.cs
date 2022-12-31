using Lavalink4NET.Player;

namespace Zeenox.Models.Events;

public class QueueChangedEventArgs : AudioEventArgs
{
    public QueueChangedEventArgs(ulong guildId, LavalinkTrack[] tracks) : base(guildId)
    {
        Tracks = tracks;
    }

    public LavalinkTrack[] Tracks { get; }
}