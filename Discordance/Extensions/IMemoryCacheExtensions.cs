using System;
using System.Collections.Generic;
using Discordance.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Discordance.Extensions;

public static class IMemoryCacheExtensions
{
    public static void SetGuildConfig(this IMemoryCache cache, GuildConfig config)
    {
        cache.Set($"{config.GuildId}:config", config, TimeSpan.FromMinutes(10));
    }

    public static GuildConfig GetGuildConfig(this IMemoryCache cache, ulong guildId)
    {
        return cache.Get<GuildConfig>($"{guildId}:config");
    }

    public static string GetLangKey(this IMemoryCache cache, ulong guildId)
    {
        return cache.GetGuildConfig(guildId).Language;
    }

    public static void SetNotificationChannels(
        this IMemoryCache cache,
        IEnumerable<(ulong, ulong)> shrine,
        IEnumerable<(ulong, ulong)> game
    )
    {
        cache.Set("shrine", shrine);
        cache.Set("game", game);
    }

    public static IEnumerable<(ulong, ulong)> GetShrineNotificationChannels(this IMemoryCache cache)
    {
        return cache.Get<IEnumerable<(ulong, ulong)>>("shrine");
    }

    public static IEnumerable<(ulong, ulong)> GetGameNotificationChannels(this IMemoryCache cache)
    {
        return cache.Get<IEnumerable<(ulong, ulong)>>("game");
    }
}
