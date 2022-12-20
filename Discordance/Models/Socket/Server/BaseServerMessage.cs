using Discordance.Enums;

namespace Discordance.Models.Socket.Server;

public struct BaseServerMessage
{
    public ServerMessageType Type { get; set; }
    public object Payload { get; set; }
}