using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Discord;
using KBot.Models;
using KBot.Modules.Gambling;
using KBot.Modules.Music;
using Lavalink4NET.Player;
using Game = KBot.Models.Game;

namespace KBot.Extensions;

public static class GenericExtensions
{
    public static DateTimeOffset GetNextWeekday(this DateTimeOffset date, DayOfWeek day)
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
            .WithButton(" ", "stop", emote: new Emoji("⏹"), row: 0)
            .WithButton(" ", "next", emote: new Emoji("⏭"), disabled: !player.CanGoForward, row: 0)
            .WithButton(" ", "volumedown", emote: new Emoji("🔉"), row: 1, disabled: player.Volume == 0)
            .WithButton(" ", "autoplay", emote: new Emoji("🔎"), row: 1)
            .WithButton(" ", "repeat", emote: new Emoji("🔁"), row: 1)
            .WithButton(" ", "clearfilters", emote: new Emoji("🗑️"), row: 1, disabled: player.FilterEnabled is null)
            .WithButton(" ", "volumeup", emote: new Emoji("🔊"), row: 1, disabled: player.Volume == 1.0f)
            .WithSelectMenu(new SelectMenuBuilder()
                .WithPlaceholder("Select Filter")
                .WithCustomId("filterselectmenu")
                .WithMinValues(1)
                .WithMaxValues(1)
                .AddOption("Bass Boost", "Bassboost", emote: new Emoji("🤔"))
                .AddOption("Pop", "Pop", emote: new Emoji("🎸"))
                .AddOption("Soft", "Soft", emote: new Emoji("✨"))
                .AddOption("Loud", "Treblebass", emote: new Emoji("🔊"))
                .AddOption("Nightcore", "Nightcore", emote: new Emoji("🌃"))
                .AddOption("8D", "Eightd", emote: new Emoji("🎧"))
                .AddOption("Chinese", "China", emote: new Emoji("🍊"))
                .AddOption("Vaporwave", "Vaporwave", emote: new Emoji("💦"))
                .AddOption("Speed Up", "Doubletime", emote: new Emoji("⏫"))
                .AddOption("Speed Down", "Slowmotion", emote: new Emoji("⏬"))
                .AddOption("Chipmunk", "Chipmunk", emote: new Emoji("🐿"))
                .AddOption("Darthvader", "Darthvader", emote: new Emoji("🤖"))
                .AddOption("Dance", "Dance", emote: new Emoji("🕺"))
                .AddOption("Vibrato", "Vibrato", emote: new Emoji("🕸"))
                .AddOption("Tremolo", "Tremolo", emote: new Emoji("📳")), 2)
            .Build();
    }

    public static string ToShortId(this Guid id)
    {
        return id.ToString().Split("-")[0];
    }

    public static long ToUnixTimeStamp(this DateTime date)
    {
        var unixTimestamp = date.Ticks - new DateTime(1970, 1, 1).Ticks;
        unixTimestamp /= TimeSpan.TicksPerSecond;
        return unixTimestamp;
    }

    public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        return source
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }

    public static EmbedBuilder ToEmbedBuilder(this IEnumerable<Perk> perks)
    {
        var eb = new EmbedBuilder()
            .WithTitle("Shrine of Secrets")
            .WithColor(Color.DarkOrange);
        foreach (var perk in perks) eb.AddField(perk.Name, $"from {perk.CharacterName}", true);
        return eb;
    }

    public static Embed[] ToEmbedArray(this IEnumerable<Game> games)
    {
        var date = ((DateTimeOffset) DateTime.Today).GetNextWeekday(DayOfWeek.Thursday).AddHours(17)
            .ToUnixTimeSeconds();
        return games.Select(game =>
            new EmbedBuilder()
                .WithTitle(game.Title)
                .WithDescription($"`{game.Description}`\n\n" +
                                 $"💰 **{game.Price.TotalPrice.FmtPrice.OriginalPrice} -> Free** \n\n" +
                                 $"🏁 <t:{date}:R>\n\n" +
                                 $"[Open in browser]({game.EpicUrl})")
                .WithImageUrl(game.KeyImages[0].Url.ToString())
                .WithColor(Color.Gold)
                .Build())
            .ToArray();
    }

    public static bool CheckIfInteractionIsPossible(this IGame? game, ulong userId, out Embed? embed)
    {
        if (game is null)
        {
            embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Game not found!**")
                .Build();
            return false;
        }

        if (game.User.Id != userId)
        {
            embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**This is not your game!**")
                .Build();
            return false;
        }

        embed = null;
        return true;
    }

    public static bool CanStartGame(this User user, int bet, out Embed? embed)
    {
        if (user.Balance < bet)
        {
            embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Insufficient balance!**")
                .AddField("Balance", $"{user.Balance.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .AddField("Bet", $"{bet.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .Build();
            return false;
        }

        if (user.MinimumBet > bet)
        {
            embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You must bet at least you minimum bet!**")
                .AddField("Minimum bet", $"{user.MinimumBet.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .AddField("Bet", $"{bet.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .Build();
            return false;
        }

        embed = null;
        return true;
    }
}