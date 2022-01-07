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
    private static IConfiguration _config;
    public static ValueTask<IConfiguration> GetConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json");
        _config = builder.Build();
        return new ValueTask<IConfiguration>(_config);
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
            Hostname = _config["LavaLinkHost"],
            Port = (ushort) int.Parse(_config["LavaLinkPort"]),
            Authorization = _config["LavaLinkPassword"]
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