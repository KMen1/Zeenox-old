using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Extensions;
using KBot.Models;
using KBot.Services;

namespace KBot.Modules.DeadByDaylight;

public class DbDService : IInjectable
{
    private readonly DiscordSocketClient _client;
    private readonly HttpClient _httpClient;
    private readonly MongoService _mongo;
    private readonly RedisService _redis;

    public DbDService(HttpClient httpClient, RedisService redisService, MongoService mongoService,
        DiscordSocketClient client)
    {
        _httpClient = httpClient;
        _redis = redisService;
        _mongo = mongoService;
        _client = client;

        Task.Run(CheckForNewShrinesAsync);
    }

    private async Task CheckForNewShrinesAsync()
    {
        var next = DateTime.UtcNow.GetNextWeekday(DayOfWeek.Thursday).AddMinutes(10).DateTime;
        await _redis.SetDbdRefreshDateAsync(next).ConfigureAwait(false);

        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            var refreshDate = await _redis.GetDbdRefreshDateAsync().ConfigureAwait(false);
            if (DateTime.UtcNow < refreshDate) continue;

            var sw = Stopwatch.StartNew();
            var shrines = await GetShrinesAsync().ConfigureAwait(false);
            sw.Stop();
            var channels = new List<ITextChannel>();
            foreach (var guild in _client.Guilds)
            {
                var config = await _mongo.GetGuildConfigAsync(guild).ConfigureAwait(false);
                if (config.DbdNotificationChannelId == 0) continue;
                var channel = guild.GetTextChannel(config.DbdNotificationChannelId);
                if (channel is null) continue;
                channels.Add(channel);
            }

            var eb = new EmbedBuilder()
                .WithTitle("Shrine of Secrets")
                .WithColor(Color.DarkOrange)
                .WithDescription($"🏁 <t:{refreshDate.AddDays(7).ToUnixTimeStamp()}:R>")
                .WithFooter($"{sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)} ms");
            foreach (var perk in shrines) eb.AddField(perk.Name, $"from {perk.CharacterName}", true);

            foreach (var textChannel in channels)
            {
                await textChannel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);
            }

            await _redis
                .SetDbdRefreshDateAsync(DateTime.UtcNow.GetNextWeekday(DayOfWeek.Thursday).AddMinutes(10).DateTime)
                .ConfigureAwait(false);
        }
    }

    public async Task<IEnumerable<Perk>> GetShrinesAsync()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36");
        var response = await _httpClient.GetStringAsync("https://dbd.onteh.net.au/api/shrine/").ConfigureAwait(false);
        var shrine = Shrines.FromJson(response);
        var perks = new List<Perk>();
        foreach (var perk in shrine.Perks)
        {
            var perkresponse = await _httpClient.GetStringAsync($"https://dbd.onteh.net.au/api/perkinfo?perk={perk.Id}")
                .ConfigureAwait(false);
            perks.Add(Perk.FromJson(perkresponse));
        }

        return perks;
    }

    public static string GetCharacterNameFromId(long jsonCharacter)
    {
        return jsonCharacter switch
        {
            0 => "Dwight Fairfield",
            1 => "Meg Thomas",
            2 => "Claudette Morel",
            3 => "Jake Park",
            4 => "Nea Karlsson",
            5 => "Laurie Strode",
            6 => "Ace Visconti",
            7 => "William \"Bill\" Overbeck",
            8 => "Feng Min",
            9 => "David King",
            10 => "Kate Denson",
            11 => "Quentin Smith",
            12 => "Detective Tapp",
            13 => "Adam Francis",
            14 => "Jeff Johansen",
            15 => "Jane Romero",
            16 => "Ashley J. Williams",
            17 => "Nancy Wheeler",
            18 => "Steve Harrington",
            19 => "Yui Kimura",
            20 => "Zarina Kassir",
            21 => "Cheryl Mason",
            22 => "Felix Richter",
            23 => "Élodie Rakoto",
            24 => "Yun-Jin Lee",
            25 => "Jill Valentine",
            26 => "Leon S. Kennedy",
            27 => "Mikaela Reid",
            28 => "Jonah Vasquez",
            29 => "Yoichi Asakawa",
            268435456 => "The Trapper",
            268435457 => "The Wraith",
            268435458 => "The Hillbilly",
            268435459 => "The Nurse",
            268435460 => "The Hag",
            268435461 => "The Shape",
            268435462 => "The Doctor",
            268435463 => "The Huntress",
            268435464 => "The Cannibal",
            268435465 => "The Nightmare",
            268435466 => "The Pig",
            268435467 => "The Clown",
            268435468 => "The Spirit",
            268435469 => "The Legion",
            268435470 => "The Plague",
            268435471 => "The Ghost Face",
            268435472 => "The Demogorgon",
            268435473 => "The Oni",
            268435474 => "The Deathslinger",
            268435475 => "The Executioner",
            268435476 => "The Blight",
            268435477 => "The Twins",
            268435478 => "The Trickster",
            268435479 => "The Nemesis",
            268435480 => "The Cenobite",
            268435481 => "The Artist",
            268435482 => "The Onryō",
            _ => "Ismeretlen"
        };
    }
}