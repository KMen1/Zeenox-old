namespace Discordance.Models;

public class NotificationConfig
{
    public NotificationConfig()
    {
        WelcomeInChannel = false;
        WelcomeChannelId = 0;
        WelcomeMessage = "";
        AnnounceBan = false;
        BanChannelId = 0;
        BanMessage = "";
        AnnounceUnban = false;
        UnbanChannelId = 0;
        UnbanMessage = "";
        AnnounceLeave = false;
        GoodbyeChannelId = 0;
        GoodbyeMessage = "";
        SendWelcomeImage = true;
        WeeklyFreeGames = false;
        FreeGameChannelId = 0;
        WeeklyShrine = false;
        ShrineChannelId = 0;
    }

    public bool WelcomeInChannel { get; set; }
    public ulong WelcomeChannelId { get; set; }
    public string WelcomeMessage { get; set; }
    public bool AnnounceBan { get; set; }
    public ulong BanChannelId { get; set; }
    public string BanMessage { get; set; }
    public bool AnnounceUnban { get; set; }
    public ulong UnbanChannelId { get; set; }
    public string UnbanMessage { get; set; }
    public bool AnnounceLeave { get; set; }
    public ulong GoodbyeChannelId { get; set; }
    public string GoodbyeMessage { get; set; }
    public bool SendWelcomeImage { get; set; }
    public bool WeeklyFreeGames { get; set; }
    public ulong FreeGameChannelId { get; set; }
    public bool WeeklyShrine { get; set; }
    public ulong ShrineChannelId { get; set; }
}