using Discord;
using Discord.Webhook;
using Newtonsoft.Json;

namespace KBot.Epic
{
    internal class Program
    {
        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync("https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=HU&allowCountries=HU").ConfigureAwait(false);
            var store = JsonConvert.DeserializeObject<StoreModel>(response);
            var eb = new EmbedBuilder()
                .WithTitle(store!.Data.Catalog.Search.Games[0].Title)
                .WithDescription($"`{store.Data.Catalog.Search.Games[0].Description}`\n\n" +
                                 $"💰 **{store.Data.Catalog.Search.Games[0].Price.TotalPrice.CountryPrice.OriginalPrice} -> Ingyenes** \n\n" +
                                 $"🏁 <t:{((DateTimeOffset)store.Data.Catalog.Search.Games[0].Promotions.PromotionalOffers[0].Offers[0].EndDate).ToUnixTimeSeconds()}:R>" +
                                 $"\n\n[Böngésző]({store.Data.Catalog.Search.Games[0].Url}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={store.Data.Catalog.Search.Games[0].UrlSlug})")
                .WithImageUrl(store.Data.Catalog.Search.Games[0].Images[0].Url)
                .WithColor(Color.Gold)
                .Build();
            using var webhookClient = new DiscordWebhookClient("https://discord.com/api/webhooks/940299160098324521/h_rKEfBRK_xyUDK2RREw9a_r3POmVlxe29o7vW8uvxGFCk17-ouQfvELhI_P2MxZero6");
            await webhookClient.SendMessageAsync(embeds: new[] { eb});
        }
    }
}