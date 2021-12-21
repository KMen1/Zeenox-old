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

    public static Task<DiscordSocketConfig> GetClientConfig()
    {
        return Task.Run(() =>
        {
            var config = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            };
            return config;
        });
    }

    public static Task<LavaConfig> GetLavaConfig()
    {
        return Task.Run(() =>
        {
            var config = new LavaConfig
            {
                Hostname = Config["LavaLinkHost"],
                Port = (ushort) int.Parse(Config["LavaLinkPort"]),
                Authorization = Config["LavaLinkPassword"]
            };
            return config;
        });
    }

    public static Task<InteractionServiceConfig> GetInteractionConfig()
    {
        return Task.Run(() =>
        {
            var config = new InteractionServiceConfig
            {
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug,
                UseCompiledLambda = true
            };
            return config;
        });
    }
}