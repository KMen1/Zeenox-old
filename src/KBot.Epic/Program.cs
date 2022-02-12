using Discord;
using Discord.Webhook;

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
                                 $"🏁 <t:{((DateTimeOffset)DateTime.Today.AddDays(7).AddHours(17)).ToUnixTimeSeconds()}:R>" +
                                 $"\n\n[Böngésző]({store.CurrentGame.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={store.CurrentGame.UrlSlug})")
                .WithImageUrl(store.CurrentGame.KeyImages[0].Url.ToString())
                .WithColor(Color.Gold)
                .Build();
            using var webhookClient = new DiscordWebhookClient("https://discord.com/api/webhooks/941756960485830746/xREzi6nMSw87h8WPBBL0Xy6aQpgxnmaErsBW-ohpASWJj4KI01VHGJvCSaLU8rnMXJOs");
            await webhookClient.SendMessageAsync("@here",embeds: new[] { eb});
        }
    }
}