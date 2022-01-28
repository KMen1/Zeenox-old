using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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

    private DiscordSocketClient Client { get; set; }
    private InteractionService InteractionService { get; set; }
    private LavaNode LavaNode { get; set; }
    private LogService LogService { get; set; }
    private AudioService AudioService { get; set; }
    private ConfigModel.Config Config { get; set; }
    private DatabaseService Database { get; set; }
    private IServiceProvider Services { get; set; }

    public async Task StartAsync()
    {
        Config = await ConfigService.InitializeAsync().ConfigureAwait(false);

        Client = new DiscordSocketClient(await ConfigService.GetClientConfig().ConfigureAwait(false));

        Database = new DatabaseService(Config, Client);

        InteractionService = new InteractionService(Client, await ConfigService.GetInteractionConfig().ConfigureAwait(false));

        LavaNode = new LavaNode(Client, await ConfigService.GetLavaConfig().ConfigureAwait(false));

        AudioService = new AudioService(Client, LavaNode);
        if (Config.Lavalink.Enabled)
        {
            AudioService.Initialize();
        }

        if (Config.Announcements.Enabled)
        {
            new AnnouncementsModule(Client, Config).Initialize();
        }

        if (Config.Movie.Enabled)
        {
            new MovieModule(Client, Config).Initialize();
        }

        if (Config.Tour.Enabled)
        {
            new TourModule(Client, Config).Initialize();
        }

        if (Config.TemporaryVoiceChannels.Enabled)
        {
            new TemporaryVoiceModule(Client, Config).Initialize();
        }

        if (Config.Leveling.Enabled)
        {
            new LevelingModule(Client, Config, Database).Initialize();
        }

        if (Config.OsuApi.Enabled)
        {
            new OsuService(Config);
        }

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
            .BuildServiceProvider();
        return Task.CompletedTask;
    }
}