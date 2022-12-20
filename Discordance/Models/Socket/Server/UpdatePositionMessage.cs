namespace Discordance.Models.Socket.Server;

public struct UpdatePositionMessage : IServerMessage
{
    public int Position { get; init; }
}