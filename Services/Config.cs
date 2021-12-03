using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Victoria;

namespace KBot.Services
{
    public class Config
    {
        public readonly IConfiguration _config;
        public Config()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile(path: "config.json");
            _config = builder.Build();
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
                    Hostname = _config["Hostname"],
                    Port = (ushort)int.Parse(_config["Port"]),
                    Authorization = _config["Password"]
                };
                return config;
            });
        }
    }
}
