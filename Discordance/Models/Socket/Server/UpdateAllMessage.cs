using Discordance.Modules.Music;

namespace Discordance.Models.Socket.Server;

public struct UpdateAllMessage : IServerMessage
{
    public UpdatePlayerStatusMessage PlayerStatus { get; init; }
    public UpdatePositionMessage Position { get; init; }
    public UpdateCurrentTrackMessage CurrentTrack { get; init; }
    public UpdateQueueMessage Queue { get; init; }

    public static UpdateAllMessage FromMusicPlayer(MusicPlayer player)
    {
        return new UpdateAllMessage
        {
            CurrentTrack = UpdateCurrentTrackMessage.FromLavalinkTrack(player.CurrentTrack),
            PlayerStatus = UpdatePlayerStatusMessage.FromPlayer(player),
            Position = new UpdatePositionMessage {Position = (int) player.Position.Position.TotalSeconds},
            Queue = UpdateQueueMessage.FromQueue(player.Queue, UpdateQueueMessageType.Replace)
        };
    }
}