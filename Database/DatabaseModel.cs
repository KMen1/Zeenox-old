using System;
using System.Collections.Generic;
using KBot.Config;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Victoria;

namespace KBot.Database;

public class GuildModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public ulong GuildId { get; init; }

    //public GuildConfig GuildConfig { get; set; }

    public List<User> Users { get; set; }
}

public class GuildConfig
{
    public ConfigModel.AnnouncementConfig Announcements { get; set; }

    public ConfigModel.TemporaryVoiceChannelConfig TemporaryChannels { get; set; }

    public ConfigModel.MovieConfig MovieEvents { get; set; }

    public ConfigModel.TourConfig TourEvents { get; set; }

    public ConfigModel.LevelingConfig Leveling { get; set; }
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

public class AudioTrack
{
    public ulong UserId { get; set; }

    public LavaTrack Track { get; init; }
}