using Discord;
using Discord.WebSocket;
using KBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Victoria;

namespace KBot
{
    public class Bot
    {
        private DiscordSocketClient Client { get; set; }
        private LavaNode LavaNode { get; set; }
        private Logging LogService { get; set; }
        private Audio AudioService { get; set; }
        private Config ConfigService { get; set; }
        private IServiceProvider Services { get; set; }
        private IConfiguration _config;

        public Bot()
        {
            Console.Title = $"KBot - {DateTime.UtcNow.Year}";
        }
        public async Task StartAsync()
        {
            ConfigService = new Config();
            var config = ConfigService._config;
            _config = config;
            Client = new DiscordSocketClient(await Config.GetClientConfig());

            LavaNode = new LavaNode(Client, await ConfigService.GetLavaConfig());

            AudioService = new Audio(Client, LavaNode);
            await AudioService.InitializeAsync();

            await GetServices();

            LogService = new Logging(Services);
            await LogService.InitializeAsync();

            var slashcommandService = new Command(Services);
            slashcommandService.InitializeAsync();

            await Client.LoginAsync(TokenType.Bot, config["Token"]);
            await Client.StartAsync();
            await Client.SetGameAsync(config["Prefix"] + config["Game"], string.Empty, ActivityType.Listening);
            await Client.SetStatusAsync(UserStatus.Online);

            await Task.Delay(-1);
        }

        private Task GetServices()
        {
            Services = new ServiceCollection()
               .AddSingleton(Client)
               .AddSingleton(LavaNode)
               .AddSingleton(AudioService)
               .AddSingleton(_config)
               .BuildServiceProvider();
            return Task.CompletedTask;
        }
    }
}
