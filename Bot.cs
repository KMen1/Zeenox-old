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
        public DiscordSocketClient _client { get; private set; }
        public LavaNode _lavaNode { get; private set; }
        public Logging _logService { get; private set; }
        public Audio _audioService { get; private set; }

        public Config _configService { get; private set; }
        public IServiceProvider _services { get; private set; }
        private IConfiguration _config;

        public Bot()
        {
            Console.Title = $"KBot - {DateTime.UtcNow.Year}";
        }
        public async Task StartAsync()
        {
            _configService = new Config();
            IConfiguration config = _configService._config;
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

        public Task GetServices()
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
