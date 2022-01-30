using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Database;

public class GuildModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public ulong GuildId { get; init; }

    public GuildConfig Config { get; set; }

    public List<User> Users { get; set; }
}

public class GuildConfig
{
    public AnnouncementConfig Announcements { get; set; }

    public TemporaryVoiceChannelConfig TemporaryChannels { get; set; }

    public MovieConfig MovieEvents { get; set; }

    public TourConfig TourEvents { get; set; }

    public LevelingConfig Leveling { get; set; }
}

public class User
{
    public ulong UserId { get; init; }

    public int Points { get; set; }

    public int Level { get; set; }

    public ulong OsuId { get; set; }

    public DateTime LastDailyClaim { get; set; }

    public DateTime LastVoiceChannelJoin { get; set; }

    public List<Warn> Warns { get; init; }
}

public class Warn
{
    public ulong ModeratorId { get; init; }

    public string Reason { get; init; }

    public DateTime Date { get; set; }
}

public class AnnouncementConfig
{
    public bool Enabled { get; set; }
    public ulong UserJoinAnnouncementChannelId { get; set; }
    public ulong UserLeaveAnnouncementChannelId { get; set; }
    public ulong UserBanAnnouncementChannelId { get; set; }
    public ulong UserUnbanAnnouncementChannelId { get; set; }
}

public class TemporaryVoiceChannelConfig
{
    public bool Enabled { get; set; }
    public ulong CategoryId { get; set; }

    public ulong CreateChannelId { get; set; }
}

public class MovieConfig
{
    public bool Enabled { get; set; }
    public ulong EventAnnouncementChannelId { get; set; }

    public ulong StreamingChannelId { get; set; }

    public ulong RoleId { get; set; }
}

public class TourConfig
{
    public bool Enabled { get; set; }
    public ulong EventAnnouncementChannelId { get; set; }

    public ulong RoleId { get; set; }
}

public class LevelingConfig
{
    public bool Enabled { get; set; }
    public int PointsToLevelUp { get; set; }

    public ulong LevelUpAnnouncementChannelId { get; set; }
}
