using System;
using KBot.Database;
using KBot.Enums;

namespace KBot.Modules.Setup.Helpers;

public static class Converters
{
    public static object GetModuleConfigFromGuildConfig(GuildModules module, GuildConfig config)
    {
        return module switch
        {
            GuildModules.Announcements => config.Announcements,
            GuildModules.TemporaryVoice => config.TemporaryChannels,
            GuildModules.Leveling => config.Leveling,
            GuildModules.MovieEvents => config.MovieEvents,
            GuildModules.TourEvents => config.TourEvents,
            GuildModules.Suggestions => config.Suggestions,
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, null)
        };
    }

    public static string GetTitleFromModuleEnum(GuildModules module)
    {
        return module switch
        {
            GuildModules.Announcements => "Bejelentések beállítása",
            GuildModules.TemporaryVoice => "Ideiglenes hangcsatornák beállítása",
            GuildModules.Leveling => "Szintrendszer beállítása",
            GuildModules.MovieEvents => "Film események beállítása",
            GuildModules.TourEvents => "Túra események beállítása",
            GuildModules.Suggestions => "Ötletek beállítása",
            _ => "Ismeretlen modul beállítása"
        };
    }
    public static string GetTitleFromPropertyName(string propertyName)
    {
        return propertyName switch
        {
            "Enabled" => "Bekapcsolva",
            "UserJoinedChannelId" => "Köszöntő csatorna",
            "JoinRoleId" => "Auto Rang",
            "UserLeftChannelId" => "Kilépő csatorna",
            "UserBannedChannelId" => "Ban csatorna",
            "UserUnbannedChannelId" => "Unban csatorna",
            "CategoryId" => "Kategória",
            "CreateChannelId" => "Létrehozás csatorna",
            "AnnouncementChannelId" => "Bejelentő csatorna",
            "StreamingChannelId" => "Vetítő csatorna",
            "RoleId" => "Rang",
            "PointsToLevelUp" => "Pontok a szintlépéshez",
            _ => "Ismeretlen"
        };
    }
}