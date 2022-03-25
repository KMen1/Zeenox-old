using System;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Setup.Helpers;

public static class Converters
{

    public static string GetTitleFromModuleEnum(ModuleType module)
    {
        return module switch
        {
            ModuleType.Announcements => "Bejelentések beállítása",
            ModuleType.TemporaryVoice => "Ideiglenes hangcsatornák beállítása",
            ModuleType.Leveling => "Szintrendszer beállítása",
            ModuleType.MovieEvents => "Film események beállítása",
            ModuleType.TourEvents => "Túra események beállítása",
            ModuleType.Suggestions => "Ötletek beállítása",
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
            "AfkChannelId" => "AFK csatorna",
            "LevelRole" => "Auto Rangok",
            _ => "Ismeretlen"
        };
    }
}