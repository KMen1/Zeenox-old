using Discord;

namespace Discordance.Models;

public struct TrackContext
{
    public IUser Requester { get; init; }
    public string CoverUrl { get; init; }
}