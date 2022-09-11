using System;
using System.Collections.Generic;

namespace Discordance.Models;

public class MusicConfig
{
    public MusicConfig()
    {
        ExclusiveControl = true;
        DjOnly = false;
        DjRoleIds = new List<ulong>();
        RequestChannelId = null;
        UseSponsorBlock = true;
        AllowedVoiceChannels = new List<ulong>();
        DefaultVolume = 100;
        PlaylistAllowed = true;
        ShowRequester = true;
        LengthLimit = TimeSpan.Zero;
    }

    public bool ExclusiveControl { get; set; }
    public bool DjOnly { get; set; }
    public List<ulong> DjRoleIds { get; set; }
    public ulong? RequestChannelId { get; set; }
    public bool UseSponsorBlock { get; set; }
    public List<ulong> AllowedVoiceChannels { get; set; }
    public int DefaultVolume { get; set; }
    public bool PlaylistAllowed { get; set; }
    public bool ShowRequester { get; set; }
    public TimeSpan LengthLimit { get; set; }
}