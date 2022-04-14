using Discord;
using Discord.Webhook;

namespace KBot.Epic;

internal static class Program
{
    private static void Main() => MainAsync().GetAwaiter().GetResult();

    private static async Task MainAsync()
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync("https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=HU").ConfigureAwait(false);
        var search = EpicStore.FromJson(response);
        var embeds = search.CurrentGame.Select(game =>
            new EmbedBuilder()
                .WithTitle(game.Title)
                .WithDescription($"`{game.Description}`\n\n" +
                                 $"💰 **{game.Price.TotalPrice.FmtPrice.OriginalPrice} -> Free** \n\n" +
                                 $"🏁 <t:{(DateTime.UtcNow.GetNextWeekday(DayOfWeek.Thursday).AddHours(17)).ToUnixTimeSeconds()}:R>\n\n" +
                                 $"[Böngésző]({game.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={game.UrlSlug})")
                .WithImageUrl(game.KeyImages[0].Url.ToString())
                .WithColor(Color.Gold).Build()).ToArray();
        using var webhookClient = new DiscordWebhookClient("https://discord.com/api/webhooks/944549106469175316/FsvlEggfDu-P4VMG-vUg2eAuc-MuNiV05ObjzF1H4oYNPE73-2Vz3Ym2O2bfELbnyfMt");
        await webhookClient.SendMessageAsync("",embeds: embeds);
    }

    private static DateTimeOffset GetNextWeekday(this DateTime date, DayOfWeek day)
    {
        var result = date.Date.AddDays(1);
        while( result.DayOfWeek != day )
            result = result.AddDays(1);
        return result;
    }
}