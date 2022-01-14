using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using EventHandler = KBot.Services.EventHandler;

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
    private ConfigService Config { get; set; }
    private IServiceProvider Services { get; set; }

    public async Task StartAsync()
    {
        Config = new ConfigService();

        Client = new DiscordSocketClient(await ConfigService.GetClientConfig());

        InteractionService = new InteractionService(Client, await ConfigService.GetInteractionConfig());
        
        LavaNode = new LavaNode(Client, await Config.GetLavaConfig());

        AudioService = new AudioService(Client, LavaNode);
        AudioService.InitializeAsync();

        await GetServices();

        LogService = new LogService(Services);
        LogService.InitializeAsync();

        var interactionHandler = new InteractionHandler(Services);
        await interactionHandler.InitializeAsync();
        
        var eventHandler = new EventHandler(Client, Config);
        eventHandler.InitializeAsync();

        await Client.LoginAsync(TokenType.Bot, Config.Token);
        await Client.StartAsync();
        await Client.SetGameAsync("/" + Config.Game, type:ActivityType.Listening);
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
            .BuildServiceProvider();
        return Task.CompletedTask;
    }
}