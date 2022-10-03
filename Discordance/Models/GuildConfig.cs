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
        TcHubs = new List<Hub>();
        SelfRoleMessages = new List<SelfRoleMessage>();
        Notifications = new NotificationConfig();
        PersistentRoles = true;
        AutoRole = false;
        AutoRoleIds = new List<ulong>();
        LevelRoles = new List<LevelRole>();
    }

    [BsonId] public ulong GuildId { get; set; }

    public string Language { get; set; }
    public MusicConfig Music { get; set; }
    public List<Hub> TcHubs { get; set; }
    public List<SelfRoleMessage> SelfRoleMessages { get; set; }
    public NotificationConfig Notifications { get; set; }
    public bool PersistentRoles { get; set; }
    public bool AutoRole { get; set; }
    public List<ulong> AutoRoleIds { get; set; }
    public List<LevelRole> LevelRoles { get; set; }
}