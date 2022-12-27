using System;

namespace Zeenox.Enums;

[Flags]
public enum ServerMessageType
{
    None = 0,
    UpdatePlayerStatus = 1,
    UpdatePosition = 2,
    UpdateCurrentTrack = 4,
    UpdateQueue = 8,
    UpdateFavorites = 16,
    UpdateAll = UpdatePlayerStatus | UpdatePosition | UpdateCurrentTrack | UpdateQueue | UpdateFavorites
}