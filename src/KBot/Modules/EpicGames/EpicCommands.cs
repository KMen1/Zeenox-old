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
    private readonly HttpClient _httpClient;
    private readonly EpicGamesService _epicGamesService;
    public EpicCommands(HttpClient httpClient, EpicGamesService epicGamesService)
    {
        _httpClient = httpClient;
        _epicGamesService = epicGamesService;
    }
    
    [SlashCommand("free", "Send the current free games on the Epic Games Store.")]
    public async Task GetEpicFreeGameAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var games = await _epicGamesService.GetCurrentFreeGamesAsync().ConfigureAwait(false);
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

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set", "Sets the channel to receive weekly epic free games.")]
    public async Task SetEpicChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.EpicNotificationChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Weekly epic notifications are now disabled**"
                : $"**Weekly epic notifications will be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}