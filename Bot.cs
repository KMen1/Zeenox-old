using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Handlers;
using KBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace KBot;

public class Bot
{
    private IConfiguration _config;

    public Bot()
    {
        Console.Title = $"KBot - {DateTime.UtcNow.Year}";
    }

    private DiscordSocketClient Client { get; set; }
    private InteractionService InteractionService { get; set; }
    private LavaNode LavaNode { get; set; }
    private LogService LogService { get; set; }
    private AudioService AudioService { get; set; }
    private ConfigService ConfigService { get; set; }
    private IServiceProvider Services { get; set; }

    public async Task StartAsync()
    {
        ConfigService = new ConfigService();
        var config = ConfigService.Config;
        _config = config;
        Client = new DiscordSocketClient(await ConfigService.GetClientConfig());

        InteractionService = new InteractionService(Client, await ConfigService.GetInteractionConfig());
        
        LavaNode = new LavaNode(Client, await ConfigService.GetLavaConfig());

        AudioService = new AudioService(Client, LavaNode);
        AudioService.InitializeAsync();

        await GetServices();

        LogService = new LogService(Services);
        LogService.InitializeAsync();

        var slashcommandService = new CommandHandler(Services);
        await slashcommandService.InitializeAsync();

        var buttonHandler = new ButtonHandler(Services);
        buttonHandler.InitializeAsync();
        
        await Client.LoginAsync(TokenType.Bot, config["Token"]);
        await Client.StartAsync();
        await Client.SetGameAsync("/" + config["Game"], string.Empty, ActivityType.Listening);
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
            .AddSingleton(_config)
            .BuildServiceProvider();
        return Task.CompletedTask;
    }
}