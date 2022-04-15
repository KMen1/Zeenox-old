using System;
using System.Security.Cryptography;
using Discord;
using KBot.Modules.Music;
using Lavalink4NET.Player;

namespace KBot.Extensions;

public static class GenericExtensions
{
    public static DateTimeOffset GetNextWeekday(this DateTime date, DayOfWeek day)
    {
        var result = date.Date.AddDays(1);
        while (result.DayOfWeek != day)
            result = result.AddDays(1);
        return result;
    }

    public static double NextDouble(this RandomNumberGenerator generator, double minimumValue, double maximumValue)
    {
        var randomNumber = new byte[1];
        generator.GetBytes(randomNumber);
        var multiplier = Math.Max(0, randomNumber[0] / 255d - 0.00000000001d);
        var range = maximumValue - minimumValue + 1;
        var randomValueInRange = Math.Floor(multiplier * range);
        return minimumValue + randomValueInRange;
    }

    public static MessageComponent NowPlayerComponents(this ComponentBuilder builder, MusicPlayer player)
    {
        return builder
            .WithButton(" ", "previous", emote: new Emoji("⏮"), disabled: !player.CanGoBack, row: 0)
            .WithButton(" ", "pause", emote: player.State == PlayerState.Playing ? new Emoji("⏸") : new Emoji("▶"),
                row: 0)
            .WithButton(" ", "stop", emote: new Emoji("⏹"), row: 0, style: ButtonStyle.Danger)
            .WithButton(" ", "next", emote: new Emoji("⏭"), disabled: !player.CanGoForward, row: 0)
            .WithButton(" ", "volumedown", emote: new Emoji("🔉"), row: 1, disabled: player.Volume == 0)
            .WithButton(" ", "repeat", emote: new Emoji("🔁"), row: 1)
            .WithButton(" ", "clearfilters", emote: new Emoji("🗑️"), row: 1)
            .WithButton(" ", "volumeup", emote: new Emoji("🔊"), row: 1, disabled: player.Volume == 1.0f)
            .WithSelectMenu(new SelectMenuBuilder()
                .WithPlaceholder("Szűrő kiválasztása")
                .WithCustomId("filterselectmenu")
                .WithMinValues(1)
                .WithMaxValues(1)
                .AddOption("Bass Boost", "Bassboost", emote: new Emoji("🤔"))
                .AddOption("Pop", "Pop", emote: new Emoji("🎸"))
                .AddOption("Soft", "Soft", emote: new Emoji("✨"))
                .AddOption("Loud", "Treblebass", emote: new Emoji("🔊"))
                .AddOption("Nightcore", "Nightcore", emote: new Emoji("🌃"))
                .AddOption("8D", "Eightd", emote: new Emoji("🧊"))
                .AddOption("Chinese", "China", emote: new Emoji("🍊"))
                .AddOption("Vaporwave", "Vaporwave", emote: new Emoji("💦"))
                .AddOption("Speed Up", "Doubletime", emote: new Emoji("⏩"))
                .AddOption("Speed Down", "Slowmotion", emote: new Emoji("⏪"))
                .AddOption("Chipmunk", "Chipmunk", emote: new Emoji("🐿"))
                .AddOption("Darthvader", "Darthvader", emote: new Emoji("🤖"))
                .AddOption("Dance", "Dance", emote: new Emoji("🕺"))
                .AddOption("Vibrato", "Vibrato", emote: new Emoji("🕸"))
                .AddOption("Tremolo", "Tremolo", emote: new Emoji("📳")), 2)
            .Build();
    }

    public static string ToShortId(this Guid guid)
    {
        return guid.ToString().Split("-")[0];
    }

    public static long ToUnixTimeStamp(this DateTime date)
    {
        var unixTimestamp = date.Ticks - new DateTime(1970, 1, 1).Ticks;
        unixTimestamp /= TimeSpan.TicksPerSecond;
        return unixTimestamp;
    }
}