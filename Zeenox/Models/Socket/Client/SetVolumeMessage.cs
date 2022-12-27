namespace Zeenox.Models.Socket.Client;

public struct SetVolumeMessage : IClientMessage
{
    public int Volume { get; init; }
}