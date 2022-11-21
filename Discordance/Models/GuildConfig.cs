using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Discordance.Models;

public class GuildConfig
{
    public GuildConfig(ulong guildId)
    {
        GuildId = guildId;
        Language = "en";
        Music = new MusicConfig();
        Hubs = new List<Hub>();
        Notifications = new NotificationConfig();
        AutoRole = false;
        AutoRoleIds = new List<ulong>();
    }

    [BsonId] public ulong GuildId { get; set; }
    public string Language { get; set; }
    public MusicConfig Music { get; set; }

    [BsonElement("TcHubs")] public List<Hub> Hubs { get; set; }

    public NotificationConfig Notifications { get; set; }
    public bool AutoRole { get; set; }
    public List<ulong> AutoRoleIds { get; set; }
}