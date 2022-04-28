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
using Serilog;
using StackExchange.Redis;
using Game = KBot.Models.Game;

namespace KBot.Modules.EpicGames;

public class EpicGamesService : IInjectable
{
    private readonly DiscordSocketClient _client;
    private readonly HttpClient _httpClient;
    private readonly MongoService _mongo;
    private readonly IConnectionMultiplexer _redis;

    public EpicGamesService(HttpClient httpClient, IConnectionMultiplexer redis, DiscordSocketClient client, MongoService mongo)
    {
        _httpClient = httpClient;
        _redis = redis;
        _client = client;
        _mongo = mongo;

        Task.Run(CheckForNewGamesAsync);
    }

    private async Task CheckForNewGamesAsync()
    {
        const string key = "next_epic_free";
        var next = DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).AddHours(17).AddMinutes(10).ToUnixTimeSeconds();
        await _redis.GetDatabase().StringSetAsync(key, next).ConfigureAwait(false);

        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            try
            {
                var value = await _redis.GetDatabase().StringGetAsync(key).ConfigureAwait(false);
                if (value.IsNull) return;
                if (!value.TryParse(out long nextUnixTime))
                    return;
                var refreshDate = DateTimeOffset.FromUnixTimeSeconds(nextUnixTime);
                
                if (DateTimeOffset.UtcNow < refreshDate) continue;

                var games = await GetCurrentFreeGamesAsync().ConfigureAwait(false);
                var channels = new List<ITextChannel>();
                foreach (var guild in _client.Guilds)
                {
                    var config = await _mongo.GetGuildConfigAsync(guild).ConfigureAwait(false);
                    if (config.EpicNotificationChannelId == 0) continue;
                    var channel = guild.GetTextChannel(config.EpicNotificationChannelId);
                    if (channel is null) continue;
                    channels.Add(channel);
                }

                var embeds = games.Select(game =>
                    new EmbedBuilder()
                        .WithTitle(game.Title)
                        .WithDescription($"`{game.Description}`\n\n" +
                                         $"💰 **{game.Price.TotalPrice.FmtPrice.OriginalPrice} -> Free** \n\n" +
                                         $"🏁 <t:{DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).AddHours(17).ToUnixTimeSeconds()}:R>\n\n" +
                                         $"[Browser]({game.EpicUrl}) • [Epic Games Launcher](http://epicfreegames.net/redirect?slug={game.UrlSlug})")
                        .WithImageUrl(game.KeyImages[0].Url.ToString())
                        .WithColor(Color.Gold).Build()).ToArray();

                foreach (var textChannel in channels)
                {
                    await textChannel.SendMessageAsync("@here", embeds: embeds).ConfigureAwait(false);
                }

                next = DateTime.Today.GetNextWeekday(DayOfWeek.Thursday).AddHours(17).AddMinutes(10).ToUnixTimeSeconds();
                await _redis.GetDatabase().StringSetAsync(key, next).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Error in EPIC loop");
            }
        }
    }

    public async Task<IEnumerable<Game>> GetCurrentFreeGamesAsync()
    {
        var response = await _httpClient
            .GetStringAsync(
                "https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=HU")
            .ConfigureAwait(false);

        var search = EpicStore.FromJson(response);
        return search.CurrentGames!;
    }
}