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
        private DiscordSocketClient _client { get; set; }
        private LavaNode _lavaNode { get; set; }
        private Logging _logService { get; set; }
        private Audio _audioService { get; set; }

        private Config _configService { get; set; }
        private IServiceProvider _services { get; set; }
        private IConfiguration _config;

        public Bot()
        {
            Console.Title = $"KBot - {DateTime.UtcNow.Year}";
        }
        public async Task StartAsync()
        {
            _configService = new Config();
            var config = _configService._config;
            _config = config;
            _client = new DiscordSocketClient(await Config.GetClientConfig());

            _lavaNode = new LavaNode(_client, await _configService.GetLavaConfig());

            _audioService = new Audio(_client, _lavaNode);
            await _audioService.InitializeAsync();

            await GetServices();

            _logService = new Logging(_services);
            await _logService.InitializeAsync();

            var slashcommandService = new Command(_services);
            slashcommandService.InitializeAsync();

            await _client.LoginAsync(TokenType.Bot, config["Token"]);
            await _client.StartAsync();
            await _client.SetGameAsync(config["Prefix"] + config["Game"], string.Empty, ActivityType.Listening);
            await _client.SetStatusAsync(UserStatus.Online);

            await Task.Delay(-1);
        }

        private Task GetServices()
        {
            _services = new ServiceCollection()
               .AddSingleton(_client)
               .AddSingleton(_lavaNode)
               .AddSingleton(_audioService)
               .AddSingleton(_config)
               .BuildServiceProvider();
            return Task.CompletedTask;
        }
    }
}
