namespace Zeenox.Models.Events;

public class PlayerStatusChangedEventArgs : AudioEventArgs
{
    public PlayerStatusChangedEventArgs(ulong guildId) : base(guildId)
    {
    }
}