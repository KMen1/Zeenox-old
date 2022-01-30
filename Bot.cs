using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using KBot.Config;
using KBot.Database;
using KBot.Modules.Announcements;
using KBot.Modules.Audio;
using KBot.Modules.Leveling;
using KBot.Modules.OSU;
using KBot.Modules.TemporaryChannels;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace KBot;

public class Bot
{
    public Bot()
    {
        Console.Title = $"KBot - {DateTime.UtcNow.Year.ToString()}";
    }

    private static DiscordSocketClient Client;
    private static LavaNode LavaNode;
    private static LogService LogService;
    private static BotConfig Config;
    private static DatabaseService Database;
    private static InteractionService InteractionService;
    private static InteractiveService InteractiveService;
    private static AudioService AudioService;
    private static IServiceProvider Services;

    public async Task StartAsync()
    {
        Config = await ConfigService.InitializeAsync().ConfigureAwait(false);

        Client = new DiscordSocketClient(await ConfigService.GetClientConfig().ConfigureAwait(false));

        Database = new DatabaseService(Config, Client);

        InteractionService = new InteractionService(Client, await ConfigService.GetInteractionConfig().ConfigureAwait(false));

        LavaNode = new LavaNode(Client, await ConfigService.GetLavaConfig().ConfigureAwait(false));

        AudioService = new AudioService(Client, LavaNode);
        AudioService.Initialize();
        InteractiveService = new InteractiveService(Client, new InteractiveConfig
        {
            DefaultTimeout = new TimeSpan(0, 0, 5, 0),
            LogLevel = LogSeverity.Debug
        });
        new AnnouncementsModule(Client, Database).Initialize();
        new MovieModule(Client, Database).Initialize();
        new TourModule(Client, Database).Initialize();
        new TemporaryVoiceModule(Client, Database).Initialize();
        new LevelingModule(Client, Database).Initialize();
        new OsuService(Config);

        await GetServicesAsync().ConfigureAwait(false);

        LogService = new LogService(Services);
        LogService.Initialize();

        await new InteractionHandler(Services).InitializeAsync().ConfigureAwait(false);

        await Client.LoginAsync(TokenType.Bot, Config.Client.Token).ConfigureAwait(false);
        await Client.StartAsync().ConfigureAwait(false);
        await Client.SetGameAsync("/" + Config.Client.Game, type: ActivityType.Listening).ConfigureAwait(false);
        await Client.SetStatusAsync(UserStatus.Online).ConfigureAwait(false);

        await Task.Delay(-1).ConfigureAwait(false);
    }

    private Task GetServicesAsync()
    {
        Services = new ServiceCollection()
            .AddSingleton(Client)
            .AddSingleton(InteractionService)
            .AddSingleton(LavaNode)
            .AddSingleton(AudioService)
            .AddSingleton(Config)
            .AddSingleton(Database)
            .AddSingleton(InteractiveService)
            .BuildServiceProvider();
        return Task.CompletedTask;
    }
}