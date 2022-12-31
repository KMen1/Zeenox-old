namespace Zeenox.Models.Socket.Client;

public struct SearchMessage : IClientMessage
{
    public string Query { get; set; }
}