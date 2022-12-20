namespace Discordance.Models.Socket.Client;

public struct SeekPositionMessage : IClientMessage
{
    public int Position { get; init; }
}