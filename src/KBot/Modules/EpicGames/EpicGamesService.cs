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
    private const string Url =
        "https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=HU";
    public IEnumerable<Game> ChachedGames { get; private set; }

    public EpicGamesService(
        HttpClient httpClient,
        IConnectionMultiplexer redis,
        DiscordSocketClient client,
        MongoService mongo
    )
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
        var next = ((DateTimeOffset)DateTime.Today)
            .GetNextWeekday(DayOfWeek.Thursday)
            .AddHours(17)
            .AddMinutes(5)
            .ToUnixTimeSeconds();
        await _redis.GetDatabase().StringSetAsync(key, next).ConfigureAwait(false);
        ChachedGames = await GetCurrentFreeGamesAsync().ConfigureAwait(false);

        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            try
            {
                var value = await _redis.GetDatabase().StringGetAsync(key).ConfigureAwait(false);
                if (value.IsNull || !value.TryParse(out long nextUnixTime))
                    continue;

                var refreshDate = DateTimeOffset.FromUnixTimeSeconds(nextUnixTime);
                if (DateTimeOffset.Now < refreshDate)
                    continue;

                var channelIds = await _mongo.GetEpicNotificationChannelIds().ConfigureAwait(false);
                var channels = channelIds
                    .Select(id => (ITextChannel)_client.GetChannel(id))
                    .Where(channel => channel is not null)
                    .ToList();

                if (channels.Count == 0)
                    continue;

                ChachedGames = await GetCurrentFreeGamesAsync().ConfigureAwait(false);
                var embeds = ChachedGames.ToEmbedArray();

                foreach (var textChannel in channels)
                {
                    await textChannel
                        .SendMessageAsync("@here", embeds: embeds)
                        .ConfigureAwait(false);
                }

                next = ((DateTimeOffset)DateTime.Today)
                    .GetNextWeekday(DayOfWeek.Thursday)
                    .AddHours(17)
                    .AddMinutes(10)
                    .ToUnixTimeSeconds();
                await _redis.GetDatabase().StringSetAsync(key, next).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Error in EPIC loop");
            }
        }
    }

    private async Task<IEnumerable<Game>> GetCurrentFreeGamesAsync()
    {
        var response = await _httpClient.GetStringAsync(Url).ConfigureAwait(false);
        var search = EpicStore.FromJson(response);
        return search.CurrentGames!;
    }
}
