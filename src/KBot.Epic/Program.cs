using Discord;
using Discord.Webhook;
using KBot.Models;

namespace KBot.Epic
{
    internal class Program
    {
        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync("https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=HU&allowCountries=HU").ConfigureAwait(false);
            var store = EpicStore.FromJson(response);
            var eb = new EmbedBuilder()
                .WithTitle(store!.CurrentGame.Title)
                .WithDescription($"`{store.CurrentGame.Description}`\n\n" +
                                 $"💰 **{store.CurrentGame.Price.TotalPrice.FmtPrice.OriginalPrice} -> Ingyenes** \n\n" +
                                 $"🏁 <t:{((DateTimeOffset) GetNextWeekday(DayOfWeek.Thursday).AddHours(17)).ToUnixTimeSeconds()}:R>" +
                                 $"\n\n[Böngésző]({store.CurrentGame.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={store.CurrentGame.UrlSlug})")
                .WithImageUrl(store.CurrentGame.KeyImages[0].Url.ToString())
                .WithColor(Color.Gold)
                .Build();
            using var webhookClient = new DiscordWebhookClient("https://discord.com/api/webhooks/944549106469175316/FsvlEggfDu-P4VMG-vUg2eAuc-MuNiV05ObjzF1H4oYNPE73-2Vz3Ym2O2bfELbnyfMt");
            await webhookClient.SendMessageAsync("@here",embeds: new[] { eb});
        }
        
        private static DateTime GetNextWeekday(DayOfWeek day)
        {
            var result = DateTime.Today.AddDays(1);
            while( result.DayOfWeek != day )
                result = result.AddDays(1);
            return result;
        }
    }
}