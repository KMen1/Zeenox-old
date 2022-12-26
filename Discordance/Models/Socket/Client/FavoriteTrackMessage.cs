namespace Discordance.Models.Socket.Client;

public struct FavoriteTrackMessage : IClientMessage
{
    public string Id { get; set; }
}