using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Zeenox.Models;

namespace Zeenox.Extensions;

public static class MemoryCacheExtensions
{
    public static void SetGuildConfig(this IMemoryCache cache, GuildConfig config)
    {
        cache.Set($"{config.GuildId}:config", config, TimeSpan.FromMinutes(10));
    }

    public static GuildConfig GetGuildConfig(this IMemoryCache cache, ulong guildId)
    {
        return cache.Get<GuildConfig>($"{guildId}:config") ?? new GuildConfig(guildId);
    }

    public static string GetLangKey(this IMemoryCache cache, ulong guildId)
    {
        return cache.GetGuildConfig(guildId).Language;
    }

    public static string GetMessage(this IMemoryCache cache, string language, string key)
    {
        var localization = cache.Get<Dictionary<string, string>>(key);
        return localization?[language] ?? localization?["en"] ?? key;
    }

    public static string GetMessage(this IMemoryCache cache, ulong guildId, string key)
    {
        var language = cache.GetGuildConfig(guildId).Language;
        return GetMessage(cache, language, key);
    }
}