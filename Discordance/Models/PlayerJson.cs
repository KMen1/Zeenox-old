using System.Linq;
using Discordance.Modules.Music;
using Lavalink4NET.Player;

namespace Discordance.Models;

public record PlayerJson(bool IsConnected, bool IsPlaying, TrackJson? CurrentTrack, TrackJson[]? Queue, bool IsPaused,
    bool IsAutoPlay, PlayerLoopMode LoopMode, int Position)
{
    public static PlayerJson FromPlayer(MusicPlayer? player)
    {
        return new PlayerJson(
            player is not null,
            player?.State is PlayerState.Paused or PlayerState.Playing,
            TrackJson.FromLavalinkTrack(player?.CurrentTrack),
            player?.Queue.Select(x => TrackJson.FromLavalinkTrack(x)).ToArray(),
            player?.State is PlayerState.Paused,
            player?.IsAutoPlay ?? false,
            player?.LoopMode ?? PlayerLoopMode.None,
            player?.CurrentTrack is null ? 0 : (int) player.Position.Position.TotalSeconds);
    }
}