using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Common;

namespace KBot.Modules.EpicFreeGames;

[Group("epic", "Epic Games parancsok")]
public class EpicCommands : KBotModuleBase
{
    [SlashCommand("free", "Elküldi a jelenleg ingyenes játékot epic games-en.")]
    public async Task GetEpicFreeGameAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        using var client = new HttpClient();
        var response = await client.GetStringAsync("https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=HU&allowCountries=HU").ConfigureAwait(false);
        var search = EpicStore.FromJson(response);
        var eb = new EmbedBuilder()
            .WithTitle(search!.CurrentGame.Title)
            .WithDescription($"`{search.CurrentGame.Description}`\n\n" +
                             $"💰 **{search.CurrentGame.Price.TotalPrice.FmtPrice.OriginalPrice} -> Ingyenes** \n\n" +
                             $"🏁 <t:{((DateTimeOffset)DateTime.Today.AddHours(17)).ToUnixTimeSeconds()}:R>" +
                             $"\n\n[Böngésző]({search.CurrentGame.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={search.CurrentGame.UrlSlug})")
            .WithImageUrl(search.CurrentGame.KeyImages[0].Url.ToString())
            .WithColor(Color.Gold)
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}