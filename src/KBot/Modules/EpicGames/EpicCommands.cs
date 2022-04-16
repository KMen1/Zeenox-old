using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.EpicGames;

[Group("epic", "Set epic channel or get free games")]
public class EpicCommands : SlashModuleBase
{
    public HttpClient HttpClient { get; set; }
    public EpicGamesService EpicGamesService { get; set; }

    [SlashCommand("free", "Send the current free games on the Epic Games Store.")]
    public async Task GetEpicFreeGameAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var games = await EpicGamesService.GetCurrentFreeGamesAsync().ConfigureAwait(false);
        var embeds = games.Select(game =>
            new EmbedBuilder()
                .WithTitle(game.Title)
                .WithDescription($"`{game.Description}`\n\n" +
                                 $"💰 **{game.Price.TotalPrice.FmtPrice.OriginalPrice} -> Free** \n\n" +
                                 $"🏁 <t:{DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).AddHours(17).ToUnixTimeSeconds()}:R>\n\n" +
                                 $"[Böngésző]({game.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={game.UrlSlug})")
                .WithImageUrl(game.KeyImages[0].Url.ToString())
                .WithColor(Color.Gold).Build()).ToArray();
        await FollowupAsync(embeds: embeds).ConfigureAwait(false);
    }

    [SlashCommand("set", "Sets the channel to receive weekly epic free games.")]
    public async Task SetEpicChannelAsync(ITextChannel channel)
    {
        await DeferAsync().ConfigureAwait(false);
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.EpicChannelId = channel.Id).ConfigureAwait(false);
        await FollowupAsync("Epic free games will now be sent to this channel.").ConfigureAwait(false);
    }
}