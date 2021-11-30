using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Victoria;

namespace KBot.Services
{
    public class Config
    {
        public static Task<DiscordSocketConfig> GetClientConfig()
        {
            return Task.Run(() =>
            {
                var config = new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    AlwaysDownloadUsers = true,
                };
                return config;
            });
        }
        public static Task<LavaConfig> GetLavaConfig()
        {
            return Task.Run(() =>
            {
                var config = new LavaConfig
                {
                    Hostname = "localhost",
                    Port = 2333,
                    Authorization = "youshallnotpass",
                };
                return config;
            });
        }
    }
}
