using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Victoria;

namespace KBot.Services
{
    public class ConfigService
    {
        public readonly IConfiguration Config;
        public ConfigService()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile(path: "config.json");
            Config = builder.Build();
        }
        public static Task<DiscordSocketConfig> GetClientConfig()
        {
            return Task.Run(() =>
            {
                var config = new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    AlwaysDownloadUsers = true
                };
                return config;
            });
        }
        public Task<LavaConfig> GetLavaConfig()
        {
            return Task.Run(() =>
            {
                var config = new LavaConfig
                {
                    Hostname = Config["Hostname"],
                    Port = (ushort)int.Parse(Config["Port"]),
                    Authorization = Config["Password"]
                };
                return config;
            });
        }
    }
}
