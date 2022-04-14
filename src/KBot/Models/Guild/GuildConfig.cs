using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models.Guild;

public class GuildConfig
{
    [BsonElement("announcements")] public AnnouncementConfig Announcements { get; set; }
    [BsonElement("moderation")] public ModerationConfig Moderation { get; set; }
    [BsonElement("temporaryvoice")] public TemporaryVoiceChannels TemporaryVoice { get; set; }
    [BsonElement("movieevents")] public MovieEvents MovieEvents { get; set; }
    [BsonElement("leveling")] public Leveling Leveling { get; set; }
    [BsonElement("suggestions")] public Suggestions Suggestions { get; set; }

    public GuildConfig()
    {
        Announcements = new AnnouncementConfig();
        Moderation = new ModerationConfig();
        TemporaryVoice = new TemporaryVoiceChannels();
        MovieEvents = new MovieEvents();
        Leveling = new Leveling();
        Suggestions = new Suggestions();
    }
}

public class ModerationConfig
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("logchannelid")] public ulong LogChannelId { get; set; }
    [BsonElement("appealchannelid")] public ulong AppealChannelId { get; set; }
}

public class AnnouncementConfig
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("joinchannelid")] public ulong JoinChannelId { get; set; }
    [BsonElement("joinroleid")] public ulong JoinRoleId { get; set; }
    [BsonElement("leftchannelid")] public ulong LeftChannelId { get; set; }
    [BsonElement("banchannelid")] public ulong BanChannelId { get; set; }
    [BsonElement("unbanchannelid")] public ulong UnbanChannelId { get; set; }

    public AnnouncementConfig()
    {
        Enabled = false;
        JoinChannelId = 0;
        JoinRoleId = 0;
        LeftChannelId = 0;
        BanChannelId = 0;
        UnbanChannelId = 0;
    }
}

public class TemporaryVoiceChannels
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("categoryid")] public ulong CategoryId { get; set; }
    [BsonElement("createchannelid")] public ulong CreateChannelId { get; set; }

    public TemporaryVoiceChannels()
    {
        Enabled = false;
        CategoryId = 0;
        CreateChannelId = 0;
    }
}

public class MovieEvents
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("announcechannelid")]public ulong AnnounceChannelId { get; set; }
    [BsonElement("streamchannelid")] public ulong StreamChannelId { get; set; }
    [BsonElement("roleid")] public ulong RoleId { get; set; }

    public MovieEvents()
    {
        Enabled = false;
        AnnounceChannelId = 0;
        StreamChannelId = 0;
        RoleId = 0;
    }
}

public class Leveling
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("announcechannelid")] public ulong AnnounceChannelId { get; set; }
    [BsonElement("afkchannelid")] public ulong AfkChannelId { get; set; }
    [BsonElement("levelroles")] public List<LevelRole> LevelRoles { get; set; }

    public Leveling()
    {
        Enabled = false;
        AnnounceChannelId = 0;
        AfkChannelId = 0;
        LevelRoles = new List<LevelRole>();
    }
}

public class Suggestions
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("announcechannelid")] public ulong AnnounceChannelId { get; set; }

    public Suggestions()
    {
        Enabled = false;
        AnnounceChannelId = 0;
    }
}