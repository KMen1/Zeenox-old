namespace Zeenox.Models.Socket.Client;

public struct PlayQueryMessage : IClientMessage
{
    public string Query { get; init; }
}