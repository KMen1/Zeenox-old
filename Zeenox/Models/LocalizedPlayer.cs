using Microsoft.Extensions.Caching.Memory;
using Zeenox.Extensions;

namespace Zeenox.Models;

public struct LocalizedPlayer
{
    public string NowPlaying { get; }
    public string AddedBy { get; }
    public string Channel { get; }
    public string Length { get; }
    public string Volume { get; }
    public string VolumeUp { get; }
    public string VolumeDown { get; }
    public string Favorite { get; }
    public string Filter { get; }
    public string InQueue { get; }
    public string Back { get; }
    public string Skip { get; }
    public string Pause { get; }
    public string Resume { get; }
    public string Stop { get; }

    public LocalizedPlayer(IMemoryCache cache, string language)
    {
        NowPlaying = cache.GetMessage(language, "NowPlaying");
        AddedBy = cache.GetMessage(language, "AddedBy");
        Channel = cache.GetMessage(language, "Channel");
        Length = cache.GetMessage(language, "Length");
        Volume = cache.GetMessage(language, "Volume");
        VolumeUp = cache.GetMessage(language, "VolUp");
        VolumeDown = cache.GetMessage(language, "VolDown");
        Favorite = cache.GetMessage(language, "Favorite");
        Filter = cache.GetMessage(language, "Filter");
        InQueue = cache.GetMessage(language, "InQueue");
        Back = cache.GetMessage(language, "Back");
        Skip = cache.GetMessage(language, "Skip");
        Pause = cache.GetMessage(language, "Pause");
        Resume = cache.GetMessage(language, "Resume");
        Stop = cache.GetMessage(language, "Stop");
    }
}