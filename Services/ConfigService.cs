using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Victoria;

namespace KBot.Services
{
    public class ConfigService
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<DiscordSocketConfig> GetClientConfig()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var clientConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
                ExclusiveBulkDelete = true,
            };
            return clientConfig;
        }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<CommandServiceConfig> GetCommandConfig()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var commandConfig = new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Debug,
                IgnoreExtraArgs = true,
                DefaultRunMode = RunMode.Async,
            };
            return commandConfig;
        }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<LavaConfig> GetLavaConfig()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var lavaConfig = new LavaConfig
            {
                Hostname = "localhost",
                Port = 2333,
                Authorization = "youshallnotpass"
            };
            return lavaConfig;
        }
    }
}
