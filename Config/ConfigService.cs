using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Victoria;

namespace KBot.Config;

public static class ConfigService
{
    private static BotConfig _config;

    public static Task<BotConfig> InitializeAsync()
    {
        var root = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json").Build();
        _config = root.Get<BotConfig>();
        return Task.FromResult(_config);
    }

    public static ValueTask<DiscordSocketConfig> GetClientConfig()
    {
        var config = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.All,
            LogGatewayIntentWarnings = false
        };
        return new ValueTask<DiscordSocketConfig>(config);
    }

    public static ValueTask<LavaConfig> GetLavaConfig()
    {
        var config = new LavaConfig
        {
            Hostname = _config.Lavalink.Host,
            Port = _config.Lavalink.Port,
            Authorization = _config.Lavalink.Password
        };
        return new ValueTask<LavaConfig>(config);
    }

    public static ValueTask<InteractionServiceConfig> GetInteractionConfig()
    {
        var config = new InteractionServiceConfig
        {
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Debug,
            UseCompiledLambda = true,
        };
        return new ValueTask<InteractionServiceConfig>(config);
    }
}