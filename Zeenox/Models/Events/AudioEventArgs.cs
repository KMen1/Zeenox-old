using System;

namespace Zeenox.Models.Events;

public class AudioEventArgs : EventArgs
{
    public AudioEventArgs(ulong guildId)
    {
        GuildId = guildId;
    }

    public ulong GuildId { get; set; }
}