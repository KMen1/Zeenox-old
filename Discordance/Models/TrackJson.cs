using Lavalink4NET.Player;

namespace Discordance.Models;

public record TrackJson(string? Title, string? Author, string? Uri, string? ThumbnailUrl, int? Duration,
    string? Requester,
    int? Position)
{
    public static TrackJson FromLavalinkTrack(LavalinkTrack? track, int? position = null)
    {
        var context = track?.Context as TrackContext?;
        var requester = context.HasValue
            ? $"{context.Value.Requester.Username}#{context.Value.Requester.DiscriminatorValue}"
            : null;
        return new TrackJson(track?.Title, track?.Author, track?.Uri?.ToString(), context?.CoverUrl,
            (int?) track?.Duration.TotalSeconds, requester, position);
    }
}