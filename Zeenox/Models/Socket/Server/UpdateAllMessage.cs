using System.Collections.Generic;
using Zeenox.Modules.Music;

namespace Zeenox.Models.Socket.Server;

public readonly struct UpdateAllMessage : IServerMessage
{
    public UpdatePlayerStatusMessage PlayerStatus { get; init; }
    public UpdatePositionMessage Position { get; init; }
    public UpdateCurrentTrackMessage CurrentTrack { get; init; }
    public UpdateQueueMessage Queue { get; init; }
    public UpdateFavoritesMessage Favorites { get; init; }

    public static UpdateAllMessage FromMusicPlayer(MusicPlayer player, List<string> favorites)
    {
        return new UpdateAllMessage
        {
            CurrentTrack = UpdateCurrentTrackMessage.FromLavalinkTrack(player.CurrentTrack),
            PlayerStatus = UpdatePlayerStatusMessage.FromPlayer(player),
            Position = UpdatePositionMessage.FromSeconds(player.Position.Position),
            Queue = UpdateQueueMessage.FromQueue(player.Queue, UpdateQueueMessageType.Replace),
            Favorites = UpdateFavoritesMessage.Create(favorites)
        };
    }
}