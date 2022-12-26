using System;
using Discordance.Extensions;
using Lavalink4NET.Player;

namespace Discordance.Models.Socket.Server;

public struct UpdateCurrentTrackMessage : IServerMessage
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int? Duration { get; init; }
    public string? DurationString { get; init; }
    public string? Requester { get; init; }

    public static UpdateCurrentTrackMessage FromLavalinkTrack(LavalinkTrack? track, TimeSpan? sponsorBlockTime = null)
    {
        var context = track?.Context as TrackContext?;
        var duration = (int?) sponsorBlockTime?.TotalSeconds ?? (int?) track?.Duration.TotalSeconds;
        var durationString = sponsorBlockTime?.ToTimeString() ??
                             track?.Duration.ToTimeString();
        return new UpdateCurrentTrackMessage
        {
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