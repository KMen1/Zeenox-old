using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.EpicGames;

[Group("epic", "Epic Games parancsok")]
public class EpicCommands : KBotModuleBase
{
    public HttpClient HttpClient { get; set; }
    
    [SlashCommand("free", "Elküldi a jelenleg ingyenes játékot epic games-en.")]
    public async Task GetEpicFreeGameAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var response = await HttpClient.GetStringAsync("https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=HU&allowCountries=HU").ConfigureAwait(false);
        var search = EpicStore.FromJson(response);
        var embeds = search.CurrentGame.Select(game =>
            new EmbedBuilder()
                .WithTitle(game.Title)
                .WithDescription($"`{game.Description}`\n\n" +
                                 $"💰 **{game.Price.TotalPrice.FmtPrice.OriginalPrice} -> Ingyenes** \n\n" +
                                 $"🏁 <t:{(DateTime.UtcNow.GetNextWeekday(DayOfWeek.Thursday).AddHours(17)).ToUnixTimeSeconds()}:R>\n\n" +
                                 $"[Böngésző]({game.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={game.UrlSlug})")
                .WithImageUrl(game.KeyImages[0].Url.ToString())
                .WithColor(Color.Gold).Build()).ToArray();
        await FollowupAsync(embeds: embeds).ConfigureAwait(false);
    }
}