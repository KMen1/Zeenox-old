using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Extensions;
using KBot.Models;
using KBot.Services;
using Game = KBot.Models.Game;

namespace KBot.Modules.EpicGames;

public class EpicGamesService : IInjectable
{
    private readonly HttpClient _httpClient;
    private readonly RedisService _redis;
    private readonly DiscordSocketClient _client;
    private readonly MongoService _mongo;

    public EpicGamesService(HttpClient httpClient, RedisService redis, DiscordSocketClient client, MongoService mongo)
    {
        _httpClient = httpClient;
        _redis = redis;
        _client = client;
        _mongo = mongo;

        Task.Run(CheckForNewGamesAsync);
    }

    private async Task CheckForNewGamesAsync()
    {
        var next = DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).AddHours(17).AddMinutes(10).DateTime;
        await _redis.SetEpicRefreshDateAsync(next).ConfigureAwait(false);

        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(30)).ConfigureAwait(false);

            var refreshDate = await _redis.GetEpicRefreshDateAsync().ConfigureAwait(false);
            if (DateTime.UtcNow < refreshDate) continue;

            var games = await GetCurrentFreeGamesAsync().ConfigureAwait(false);
            var channels = new List<ITextChannel>();
            foreach (var guild in _client.Guilds)
            {
                var config = await _mongo.GetGuildConfigAsync(guild).ConfigureAwait(false);
                if (config.EpicNotificationChannelId != 0)
                {
                    channels.Add(await _client.GetChannelAsync(config.EpicNotificationChannelId).ConfigureAwait(false) as ITextChannel);
                }
            }
            var embeds = games.Select(game =>
                new EmbedBuilder()
                    .WithTitle(game.Title)
                    .WithDescription($"`{game.Description}`\n\n" +
                                     $"💰 **{game.Price.TotalPrice.FmtPrice.OriginalPrice} -> Free** \n\n" +
                                     $"🏁 <t:{DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).AddHours(17).ToUnixTimeSeconds()}:R>\n\n" +
                                     $"[Böngésző]({game.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={game.UrlSlug})")
                    .WithImageUrl(game.KeyImages[0].Url.ToString())
                    .WithColor(Color.Gold).Build()).ToArray();

            foreach (var textChannel in channels)
            {
                await textChannel.SendMessageAsync("@here", embeds: embeds).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);
            }
            await _redis.SetEpicRefreshDateAsync(DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).AddHours(17).AddMinutes(10).DateTime).ConfigureAwait(false);
        }
    }

    public async Task<IEnumerable<Game>> GetCurrentFreeGamesAsync()
    {
        var response = await _httpClient
            .GetStringAsync(
                "https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=HU")
            .ConfigureAwait(false);

        var search = EpicStore.FromJson(response);
        return search.CurrentGames;
    }
}