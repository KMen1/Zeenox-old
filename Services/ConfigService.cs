using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Victoria;

namespace KBot.Services;

public class ConfigService
{
    public static IConfiguration Config;

    public ConfigService()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json");
        Config = builder.Build();
    }

    public static ValueTask<DiscordSocketConfig> GetClientConfig()
    {
        var config = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.AllUnprivileged,
        };
        return new ValueTask<DiscordSocketConfig>(config);
    }

    public static ValueTask<LavaConfig> GetLavaConfig()
    {
        var config = new LavaConfig
        {
            Hostname = Config["LavaLinkHost"],
            Port = (ushort) int.Parse(Config["LavaLinkPort"]),
            Authorization = Config["LavaLinkPassword"]
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