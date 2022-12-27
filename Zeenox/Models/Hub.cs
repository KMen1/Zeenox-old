namespace Zeenox.Models;

public class Hub
{
    public Hub(ulong categoryId, ulong channelId, string channelName, int userLimit, int bitrate)
    {
        CategoryId = categoryId;
        ChannelId = channelId;
        ChannelName = channelName;
        UserLimit = userLimit;
        Bitrate = bitrate;
    }

    public ulong CategoryId { get; set; }
    public ulong ChannelId { get; set; }
    public string ChannelName { get; set; }
    public int UserLimit { get; set; }
    public int Bitrate { get; set; }
}