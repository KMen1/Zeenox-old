using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Config;
using KBot.Database;
using KBot.Modules;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace KBot;

public class Bot
{
    public Bot()
    {
        Console.Title = $"KBot - {DateTime.UtcNow.Year}";
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
        Config = await ConfigService.InitializeAsync();
        
        Client = new DiscordSocketClient(await ConfigService.GetClientConfig());
        
        Database = new DatabaseService(Config, Client);
        
        InteractionService = new InteractionService(Client, await ConfigService.GetInteractionConfig());
        
        LavaNode = new LavaNode(Client, await ConfigService.GetLavaConfig());

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

        await GetServices();

        LogService = new LogService(Services);
        LogService.InitializeAsync();

        await new InteractionHandler(Services).InitializeAsync();

        await Client.LoginAsync(TokenType.Bot, Config.Client.Token);
        await Client.StartAsync();
        await Client.SetGameAsync("/" + Config.Client.Game, type:ActivityType.Listening);
        await Client.SetStatusAsync(UserStatus.Online);

        await Task.Delay(-1);
    }
    
    private Task GetServices()
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