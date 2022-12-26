using System;
using Discordance.Extensions;

namespace Discordance.Models.Socket.Server;

public struct UpdatePositionMessage : IServerMessage
{
    public int Position { get; init; }
    public string PositionString { get; init; }

    public static UpdatePositionMessage FromSeconds(TimeSpan position)
    {
        return new UpdatePositionMessage
        {
            Position = (int) position.TotalSeconds,
            PositionString = position.ToTimeString()
        };
    }
}