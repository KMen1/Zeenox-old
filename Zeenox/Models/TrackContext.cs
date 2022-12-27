using Discord;

namespace Zeenox.Models;

public struct TrackContext
{
    public IUser Requester { get; init; }
    public string CoverUrl { get; init; }
}