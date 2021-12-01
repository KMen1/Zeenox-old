using Discord;
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
                    Hostname = "lava.link",
                    Port = 80,
                    Authorization = "youshallnotpass",
                };
                return config;
            });
        }
    }
}
