using System.Collections.Generic;
using System.IO;
using Discord;
using Discordance.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Discordance.Services;

public class LocalizationService
{
    private readonly IMemoryCache _cache;

    public LocalizationService(IMemoryCache cache)
    {
        _cache = cache;
        var localization = JsonConvert.DeserializeObject<
            Dictionary<string, Dictionary<string, string>>
        >(File.ReadAllText("Resources/Localization.json"));

        if (localization is null)
            throw new FileNotFoundException("Localization file not found");

        foreach (var message in localization)
        {
            _cache.Set(message.Key, message.Value);
        }
    }

    public string GetMessage(string language, string key)
    {
        var localization = _cache.Get<Dictionary<string, string>>(key);
        return localization?[language] ?? localization?["en"] ?? key;
    }

    public string GetMessage(ulong guildId, string key)
    {
        var language = _cache.GetGuildConfig(guildId).Language;
        return GetMessage(language, key);
    }

    public string GetMessage(IGuild guild, string key)
    {
        var language = _cache.GetGuildConfig(guild.Id).Language;
        return GetMessage(language, key);
    }
}
