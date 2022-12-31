using Zeenox.Enums;

namespace Zeenox.Models.Socket.Server;

public readonly struct BaseServerMessage
{
    public ServerMessageType Type { get; init; }
    public object Payload { get; init; }
}