using Lavalink4NET.Player;
using Zeenox.Extensions;

namespace Zeenox.Models.Socket.Server;

public readonly struct UpdateCurrentTrackMessage : IServerMessage
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int? Duration { get; init; }
    public string? DurationString { get; init; }
    public string? Requester { get; init; }

    public static UpdateCurrentTrackMessage FromLavalinkTrack(LavalinkTrack? track)
    {
        var context = track?.Context as TrackContext?;
        var duration = (int?) track?.Duration.TotalSeconds;
        var durationString = track?.Duration.ToTimeString();
        return new UpdateCurrentTrackMessage
        {
            Id = track?.Identifier,
            Title = track?.Title,
            Author = track?.Author,
            Url = track?.Uri?.ToString() ?? "",
            ThumbnailUrl = context?.CoverUrl,
            Duration = duration,
            DurationString = durationString,
            Requester = $"{context?.Requester.Username}#{context?.Requester.Discriminator}"
        };
    }
}