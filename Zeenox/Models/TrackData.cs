using Lavalink4NET.Player;
using Zeenox.Extensions;

namespace Zeenox.Models;

public struct TrackData
{
    public string? Id { get; set; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int? Duration { get; init; }
    public string? DurationString { get; init; }
    public string? Requester { get; init; }
    public int? QueuePosition { get; init; }

    public static TrackData FromLavalinkTrack(LavalinkTrack? track, int? queuePosition = null)
    {
        if (track is null)
            return default;

        var context = track.Context as TrackContext?;
        var requester = context.HasValue
            ? $"{context.Value.Requester.Username}#{context.Value.Requester.DiscriminatorValue}"
            : null;
        return new TrackData
        {
            Id = track.Identifier,
            Title = track.Title,
            Author = track.Author,
            Url = track.Uri?.ToString(),
            ThumbnailUrl = context?.CoverUrl,
            Duration = (int?) track.Duration.TotalSeconds,
            DurationString = track.Duration.ToTimeString(),
            Requester = requester,
            QueuePosition = queuePosition
        };
    }
}