using System.Collections.Generic;
using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class GuildConfig
{
    public GuildConfig(IGuild guild)
    {
        GuildId = guild.Id;
        WelcomeChannelId = 0;
        WelcomeRoleId = 0;
        LeaveChannelId = 0;
        BanChannelId = 0;
        UnbanChannelId = 0;
        ModLogChannelId = 0;
        TemporaryVoiceCategoryId = 0;
        TemporaryVoiceCreateId = 0;
        LevelUpChannelId = 0;
        AfkChannelId = 0;
        LevelRoles = new List<LevelRole>();
        SuggestionChannelId = 0;
        EpicNotificationChannelId = 0;
        DbdNotificationChannelId = 0;
    }
    
    [BsonId] public ulong GuildId { get; set; }
    [BsonElement("welcome_channel_id")] public ulong WelcomeChannelId { get; set; }
    [BsonElement("welcome_role_id")] public ulong WelcomeRoleId { get; set; }
    [BsonElement("leave_channel_id")] public ulong LeaveChannelId { get; set; }
    [BsonElement("ban_channel_id")] public ulong BanChannelId { get; set; }
    [BsonElement("unban_channel_id")] public ulong UnbanChannelId { get; set; }
    [BsonElement("modlog_channel_id")] public ulong ModLogChannelId { get; set; }
    [BsonElement("appeal_channel_id")] public ulong AppealChannelId { get; set; }

    [BsonElement("temporary_voice_category_id")] public ulong TemporaryVoiceCategoryId { get; set; }
    [BsonElement("temporary_voice_create_id")] public ulong TemporaryVoiceCreateId { get; set; }

    [BsonElement("levelup_channel_id")] public ulong LevelUpChannelId { get; set; }
    [BsonElement("afk_channel_id")] public ulong AfkChannelId { get; set; }
    [BsonElement("level_roles")] public List<LevelRole> LevelRoles { get; set; }

    [BsonElement("suggestion_channel_id")] public ulong SuggestionChannelId { get; set; }
    [BsonElement("epic_notification_channel_id")] public ulong EpicNotificationChannelId { get; set; }
    [BsonElement("dbd_notification_channel_id")] public ulong DbdNotificationChannelId { get; set; }
}