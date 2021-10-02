using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KBot.Commands;
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
        public CommandService _commandService { get; private set; }
        public LavaNode _lavaNode { get; private set; }
        public LogService _logService { get; private set; }
        public ConfigService _configService { get; private set; }
        public AudioService _audioService { get; private set; }
        public IServiceProvider _services { get; private set; }
        private readonly IConfiguration _config;

        public Bot()
        {
            Console.Title = "KBot - 2021";
            var _builder = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile(path: "config.json");
            _config = _builder.Build();
        }
        public async Task StartAsync()
        {
            _configService = new ConfigService();

            _client = new DiscordSocketClient(await _configService.GetClientConfig().ConfigureAwait(false));

            _commandService = new CommandService(await _configService.GetCommandConfig().ConfigureAwait(false));

            _lavaNode = new LavaNode(_client, await _configService.GetLavaConfig().ConfigureAwait(false));

            _audioService = new AudioService(_client, _lavaNode);
            await _audioService.InitializeAsync();

            await GetServices();

            _logService = new LogService(_services);
            await _logService.InitializeAsync();

            _ = new Help(_commandService);
            var commandService = new CommandHandler(_services);
            await commandService.InitializeAsync();

            await _client.LoginAsync(TokenType.Bot, _config["Token"]);
            await _client.StartAsync();
            await _client.SetGameAsync(_config["Prefix"] + _config["Game"], string.Empty, ActivityType.Listening);
            await _client.SetStatusAsync(UserStatus.Online);

            await Task.Delay(-1);
        }

        public Task GetServices()
        {
            _services = new ServiceCollection()
               .AddSingleton(_client)
               .AddSingleton(_commandService)
               .AddSingleton(_lavaNode)
               .AddSingleton(_audioService)
               .AddSingleton(_config)
               .BuildServiceProvider();
            return Task.CompletedTask;
        }
    }
}
