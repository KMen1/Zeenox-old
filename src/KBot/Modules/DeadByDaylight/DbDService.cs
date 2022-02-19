﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using KBot.Modules.DeadByDaylight.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KBot.Modules.DeadByDaylight;

public class DbDService
{
    private readonly IMemoryCache _cache;

    public DbDService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<(List<Perk> Perks, long EndTime)> GetWeeklyShrinesAsync()
    {
        var shrines = await _cache.GetOrCreateAsync("WeeklyShrines", async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await RefreshWeeklyShrinesAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
        if (UnixTimestampFromDateTime(DateTime.UtcNow) > shrines.EndTime)
        {
            shrines = await RefreshWeeklyShrinesAsync().ConfigureAwait(false);
        }
        return shrines;
    }

    private static async Task<(List<Perk> Perks, long EndTime)> RefreshWeeklyShrinesAsync()
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36");
        var response = await httpClient.GetStringAsync("https://dbd.onteh.net.au/api/shrine/").ConfigureAwait(false);
        var shrine = Shrines.FromJson(response);
        var perks = new List<Perk>();
        foreach (var perk in shrine.Perks)
        {
            var perkresponse = await httpClient.GetStringAsync($"https://dbd.onteh.net.au/api/perkinfo?perk={perk.Id}").ConfigureAwait(false);
            perks.Add(Perk.FromJson(perkresponse));
        }
        return (perks, shrine.End);
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
    private string GetCharacterIdFromNumberId(long jsonCharacter)
    {
        return jsonCharacter switch
        {
            0 => "Dwight",
            1 => "Meg",
            2 => "Claudette",
            3 => "Jake",
            4 => "Nea",
            5 => "Laurie",
            6 => "Ace",
            7 => "Bill",
            8 => "Feng",
            9 => "Smoke",
            10 => "Kate",
            11 => "Quentin",
            12 => "Eric",
            13 => "Adam",
            14 => "Jeff",
            15 => "Jane",
            16 => "Ash",
            17 => "Nancy",
            18 => "Steve",
            19 => "Yui",
            20 => "Zarina",
            21 => "S22",
            22 => "S23",
            23 => "S24",
            24 => "S25",
            25 => "S26",
            26 => "S27",
            27 => "S28",
            28 => "S29",
            29 => "S30",
            268435456 => "Chuckles",
            268435457 => "Bob",
            268435458 => "HillBilly",
            268435459 => "Nurse",
            268435460 => "Witch",
            268435461 => "Shape",
            268435462 => "Killer07",
            268435463 => "Bear",
            268435464 => "Cannibal",
            268435465 => "Nightmare",
            268435466 => "Pig",
            268435467 => "Clown",
            268435468 => "Spirit",
            268435469 => "Legion",
            268435470 => "Plague",
            268435471 => "Ghostface",
            268435472 => "Demogorgon",
            268435473 => "Oni",
            268435474 => "Gunslinger",
            268435475 => "K20",
            268435476 => "K21",
            268435477 => "K22",
            268435478 => "K23",
            268435479 => "K24",
            268435480 => "K25",
            268435481 => "K26",
            268435482 => "K27",
            _ => "Ismeretlen"
        };
    }
    
    private static DateTime TimeFromUnixTimestamp(int unixTimestamp)
    {
        DateTime unixYear0 = new DateTime(1970, 1, 1);
        long unixTimeStampInTicks = unixTimestamp * TimeSpan.TicksPerSecond;
        DateTime dtUnix = new DateTime(unixYear0.Ticks + unixTimeStampInTicks);
        return dtUnix;
    }
    public static long UnixTimestampFromDateTime(DateTime date)
    {
        long unixTimestamp = date.Ticks - new DateTime(1970, 1, 1).Ticks;
        unixTimestamp /= TimeSpan.TicksPerSecond;
        return unixTimestamp;
    }
}