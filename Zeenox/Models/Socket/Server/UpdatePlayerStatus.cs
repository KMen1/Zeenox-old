using System;
using Lavalink4NET.Player;
using Zeenox.Modules.Music;

namespace Zeenox.Models.Socket.Server;

public readonly struct UpdatePlayerStatusMessage : IServerMessage
{
    public bool IsConnected { get; init; }
    public bool IsPlaying { get; init; }
    public bool IsPaused { get; init; }
    public bool IsAutoplay { get; init; }
    public PlayerLoopMode LoopMode { get; init; }
    public int Volume { get; init; }
    public string Filter { get; init; }

    public static UpdatePlayerStatusMessage FromPlayer(MusicPlayer player)
    {
        return new UpdatePlayerStatusMessage
        {
            IsConnected = player.State is not PlayerState.NotConnected or PlayerState.Destroyed,
            IsPlaying = player.State is PlayerState.Playing,
            IsPaused = player.State is PlayerState.Paused,
            IsAutoplay = player.IsAutoPlay,
            LoopMode = player.LoopMode,
            Volume = (int) Math.Round(player.Volume * 100),
            Filter = player.CurrentFilter
        };
    }
}