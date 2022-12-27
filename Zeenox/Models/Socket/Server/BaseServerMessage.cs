using Zeenox.Enums;

namespace Zeenox.Models.Socket.Server;

public struct BaseServerMessage
{
    public ServerMessageType Type { get; set; }
    public object Payload { get; set; }
}