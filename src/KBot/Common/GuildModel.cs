using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Common;

public class GuildModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public ulong GuildId { get; set; }

    public GuildConfig Config { get; set; }

    public List<User> Users { get; set; }
}

public class GuildConfig
{
    public AnnouncementConfig Announcements { get; set; }

    public TemporaryChannels TemporaryChannels { get; set; }

    public MovieEvents MovieEvents { get; set; }

    public TourEvents TourEvents { get; set; }

    public Leveling Leveling { get; set; }
    public Suggestions Suggestions { get; set; }
}

public class Suggestions
{
    public bool Enabled { get; set; }
    public ulong AnnouncementChannelId { get; set; }
}

public class User
{
    public ulong UserId { get; set; }

    public int Points { get; set; }

    public int Level { get; set; }

    public ulong OsuId { get; set; }

    public DateTime LastDailyClaim { get; set; }

    public DateTime LastVoiceChannelJoin { get; set; }

    public List<Warn> Warns { get; set; }
}

public class Warn
{
    public Warn(ulong moderatorId, string reason, DateTime date)
    {
        ModeratorId = moderatorId;
        Reason = reason;
        Date = date;
    }
    public ulong ModeratorId { get; }

    public string Reason { get; }

    public DateTime Date { get; }
}

public class AnnouncementConfig
{
    public bool Enabled { get; set; }
    public ulong UserJoinedChannelId { get; set; }
    public ulong JoinRoleId { get; set; }
    public ulong UserLeftChannelId { get; set; }
    public ulong UserBannedChannelId { get; set; }
    public ulong UserUnbannedChannelId { get; set; }
}

public class TemporaryChannels
{
    public bool Enabled { get; set; }
    public ulong CategoryId { get; set; }

    public ulong CreateChannelId { get; set; }
}

public class MovieEvents
{
    public bool Enabled { get; set; }
    public ulong AnnouncementChannelId { get; set; }

    public ulong StreamingChannelId { get; set; }

    public ulong RoleId { get; set; }
}

public class TourEvents
{
    public bool Enabled { get; set; }
    public ulong AnnouncementChannelId { get; set; }

    public ulong RoleId { get; set; }
}

public class Leveling
{
    public bool Enabled { get; set; }
    public int PointsToLevelUp { get; set; }

    public ulong AnnouncementChannelId { get; set; }
    
    public ulong AfkChannelId { get; set; }

    public List<LevelRole> LevelRoles { get; set; }
}

public class LevelRole
{
    public LevelRole(int level, ulong roleId)
    {
        Level = level;
        RoleId = roleId;
    }
    public int Level { get; }
    public ulong RoleId { get; }
}
