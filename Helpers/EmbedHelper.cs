using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Victoria;

namespace KBot.Helpers
{
    public static class EmbedHelper
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<Embed> MakeEmbed(DiscordSocketClient client, LavaPlayer player, SocketUser user, string AuthorName, string Description, Color color)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var eb = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = AuthorName,
                    IconUrl = client.CurrentUser.GetAvatarUrl()
                },
                Description = Description,
                Color = color,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username} | {DateTime.UtcNow}",
                    IconUrl = user.GetAvatarUrl()
                }
            };

            if (player != null)
            {
                eb.WithTitle(player.Track.Title);
                eb.WithUrl(player.Track.Url);
            }

            return eb.Build();
        }
    }
}
