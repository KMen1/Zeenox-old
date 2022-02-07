using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json;

namespace KBot.Modules.EpicFreeGames;

[Group("epic", "Epic Games parancsok")]
public class EpicCommands : KBotModuleBase
{
    [SlashCommand("free", "Elküldi a jelenleg ingyenes játékot epic games-en.")]
    public async Task GetEpicFreeGameAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        using (var client = new HttpClient())
        {
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
            await FollowupAsync(embed: eb).ConfigureAwait(false);
        }
    }
}