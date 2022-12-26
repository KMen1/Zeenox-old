namespace Discordance.Models.Socket.Client;

public struct RemoveTrackFromQueueMessage : IClientMessage
{
    public int Index { get; set; }
}