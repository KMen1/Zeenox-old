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
    public readonly ulong UserAnnouncementChannelId;
    public readonly ulong TourAnnouncementChannelId;
    public readonly ulong MovieEventAnnouncementChannelId;
    public readonly ulong MovieStreamingChannelId;
    
    public readonly ulong MovieRoleId;
    public readonly ulong TourRoleId;

    public readonly string Token;
    public readonly string Game;

    private readonly string LavalinkHost;
    private readonly ushort LavalinkPort;
    private readonly string LavalinkPassword;

    public ConfigService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json").Build();

        UserAnnouncementChannelId = ulong.Parse(config["UserAnnouncementChannelId"]);
        TourAnnouncementChannelId = ulong.Parse(config["TourAnnouncementChannelId"]);
        MovieEventAnnouncementChannelId = ulong.Parse(config["MovieEventAnnouncementChannelId"]);
        MovieStreamingChannelId = ulong.Parse(config["MovieStreamingChannelId"]);
        MovieRoleId = ulong.Parse(config["MovieRoleId"]);
        TourRoleId = ulong.Parse(config["TourRoleId"]);
        
        Token = config["Token"];
        Game = config["Game"];
        
        LavalinkHost = config["LavalinkHost"];
        LavalinkPort = ushort.Parse(config["LavalinkPort"]);
        LavalinkPassword = config["LavalinkPassword"];
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

    public ValueTask<LavaConfig> GetLavaConfig()
    {
        var config = new LavaConfig
        {
            Hostname = LavalinkHost,
            Port = LavalinkPort,
            Authorization = LavalinkPassword
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