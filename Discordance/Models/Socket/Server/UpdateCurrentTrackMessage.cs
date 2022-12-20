using Lavalink4NET.Player;

namespace Discordance.Models.Socket.Server;

public struct UpdateCurrentTrackMessage : IServerMessage
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int? Duration { get; init; }
    public string? Requester { get; init; }

    public static UpdateCurrentTrackMessage FromLavalinkTrack(LavalinkTrack? track)
    {
        var context = track?.Context as TrackContext?;
        return new UpdateCurrentTrackMessage
        {
            Title = track?.Title,
            Author = track?.Author,
            Url = track?.Uri?.ToString() ?? "",
            ThumbnailUrl = context?.CoverUrl,
            Duration = (int?) track?.Duration.TotalSeconds,
            Requester = $"{context?.Requester.Username}#{context?.Requester.Discriminator}"
        };
    }
}