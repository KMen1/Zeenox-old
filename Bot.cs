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
        
        Database = new DatabaseService(Config);
        
        Client = new DiscordSocketClient(await ConfigService.GetClientConfig());

        InteractionService = new InteractionService(Client, await ConfigService.GetInteractionConfig());
        
        LavaNode = new LavaNode(Client, await ConfigService.GetLavaConfig());

        AudioService = new AudioService(Client, LavaNode);
        if (Config.Lavalink.Enabled)
        {
            AudioService.InitializeAsync();
        }

        if (Config.Announcements.Enabled)
        {
            await new Announcements(Client, Config).InitializeAsync();
        }

        if (Config.Movie.Enabled)
        {
            await new MovieModule(Client, Config).InitializeAsync();
        }
        
        if (Config.Tour.Enabled)
        {
            await new TourModule(Client, Config).InitializeAsync();
        }
        
        if (Config.TemporaryVoiceChannels.Enabled)
        {
            await new TemporaryVoiceModule(Client, Config).InitializeAsync();
        }

        if (Config.Leveling.Enabled)
        {
            await new LevelingModule(Client, Config, Database).InitializeAsync();
        }

        await GetServices();

        LogService = new LogService(Services);
        LogService.InitializeAsync();

        var interactionHandler = new InteractionHandler(Services);
        await interactionHandler.InitializeAsync();

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