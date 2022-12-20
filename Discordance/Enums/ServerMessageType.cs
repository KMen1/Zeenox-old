using System;

namespace Discordance.Enums;

[Flags]
public enum ServerMessageType
{
    None = 0,
    UpdatePlayerStatus = 1,
    UpdatePosition = 2,
    UpdateCurrentTrack = 4,
    UpdateQueue = 8,
    UpdateAll = UpdatePlayerStatus | UpdatePosition | UpdateCurrentTrack | UpdateQueue
}