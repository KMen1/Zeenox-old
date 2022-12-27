﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Discord;
using Discord.WebSocket;
using Lavalink4NET.Player;
using SkiaSharp;
using Zeenox.Models;
using Zeenox.Models.Games;

namespace Zeenox.Extensions;

public static class GenericExtensions
{
    public static DateTimeOffset GetNextWeekday(this DateTimeOffset date, DayOfWeek day)
    {
        var result = date.Date.AddDays(1);
        while (result.DayOfWeek != day)
            result = result.AddDays(1);
        return result;
    }

    public static double NextDouble(
        this RandomNumberGenerator generator,
        double minimumValue,
        double maximumValue
    )
    {
        var randomNumber = new byte[1];
        generator.GetBytes(randomNumber);
        var multiplier = Math.Max(0, randomNumber[0] / 255d - 0.00000000001d);
        var range = maximumValue - minimumValue + 1;
        var randomValueInRange = Math.Floor(multiplier * range);
        return minimumValue + randomValueInRange;
    }

    public static string ToShortId(this Guid id)
    {
        return id.ToString().Split("-")[0];
    }

    public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        return source
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }

    public static bool CanStartGame(this User user, int bet, out Embed? embed)
    {
        if (user.Balance < bet)
        {
            embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Insufficient balance!**")
                .AddField(
                    "Balance",
                    $"{user.Balance.ToString("N0", CultureInfo.InvariantCulture)}",
                    true
                )
                .AddField("Bet", $"{bet.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .Build();
            return false;
        }

        var minimumBet = user.GetMinimumBet();

        if (bet < minimumBet)
        {
            embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You must bet at least your minimum bet!**")
                .AddField(
                    "Minimum bet",
                    $"{minimumBet.ToString("N0", CultureInfo.InvariantCulture)}",
                    true
                )
                .AddField("Bet", $"{bet.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .Build();
            return false;
        }

        embed = null;
        return true;
    }

    public static SKBitmap MakeImageRound(this SKBitmap image)
    {
        var roundedImage = new SKBitmap(image.Width, image.Height);
        using var canvas = new SKCanvas(roundedImage);
        canvas.Clear(SKColors.Transparent);
        using var path = new SKPath();
        // ReSharper disable PossibleLossOfFraction
        path.AddCircle(image.Width / 2, image.Height / 2, image.Width / 2 - 1f);
        canvas.ClipPath(path, SKClipOperation.Intersect, true);
        canvas.DrawBitmap(image, new SKPoint(0, 0));
        return roundedImage;
    }

    public static string ParseWelcomeMessage(string message, SocketGuildUser user)
    {
        message = message.Replace(
            "{user.mention}",
            user.Mention,
            StringComparison.OrdinalIgnoreCase
        );
        message = message.Replace("{user.name}", user.Username, StringComparison.OrdinalIgnoreCase);
        message = message.Replace(
            "{user.discriminator}",
            user.Discriminator,
            StringComparison.OrdinalIgnoreCase
        );
        message = message.Replace(
            "{user.nickname}",
            user.Nickname,
            StringComparison.OrdinalIgnoreCase
        );
        message = message.Replace(
            "{guild.name}",
            user.Guild.Name,
            StringComparison.OrdinalIgnoreCase
        );
        message = message.Replace(
            "{guild.count}",
            user.Guild.Users.Count.ToString("N0", CultureInfo.InvariantCulture),
            StringComparison.OrdinalIgnoreCase
        );
        return message;
    }

    public static string ParseMessage(string message, IUser user, IGuild? guild)
    {
        message = message.Replace("{user.name}", user.Username, StringComparison.OrdinalIgnoreCase);
        message = message.Replace(
            "{user.discriminator}",
            user.Discriminator,
            StringComparison.OrdinalIgnoreCase
        );
        message = message.Replace("{guild.name}", guild?.Name, StringComparison.OrdinalIgnoreCase);
        return message;
    }

    public static string ToTimeString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
            return $"00:{timeSpan.Seconds:00}";
        var sb = new StringBuilder();
        if (timeSpan.Days > 0)
            sb.Append($"{timeSpan.Days}:");
        if (timeSpan.Hours > 0)
            sb.Append($"{timeSpan.Hours.ToString("00")}:");
        if (timeSpan.Minutes > 0)
            sb.Append($"{timeSpan.Minutes.ToString("00")}:");
        sb.Append(timeSpan.Seconds.ToString("00"));
        return sb.ToString();
    }

    private static string ToRelativeDiscordTimeStamp(this DateTimeOffset dateTimeOffset)
    {
        return $"<t:{dateTimeOffset.ToUnixTimeSeconds()}:R>";
    }

    public static string ToHyperLink(this LavalinkTrack track)
    {
        return $"[{track.Title}]({track.Uri})";
    }

    public static string FormatWithTimestamp(this string format, object? object1)
    {
        return string.Format(format, DateTimeOffset.UtcNow.ToRelativeDiscordTimeStamp(), object1);
    }

    public static string FormatWithTimestamp(
        this string format,
        object? object1,
        object? object2
    )
    {
        return string.Format(format, DateTimeOffset.UtcNow.ToRelativeDiscordTimeStamp(), object1, object2);
    }

    public static string Format(this string format, params object[] args)
    {
        return string.Format(format, args);
    }

    public static string Format(this string format, object? object1, object? object2)
    {
        return string.Format(format, object1, object2);
    }

    public static string Format(this string format, object? object1)
    {
        return string.Format(format, object1);
    }

    public static int GetValue(this IReadOnlyCollection<Card> cards)
    {
        var aces = cards.Count(x => x.Face is Face.Ace);
        var value = cards.Sum(card => card.Value);

        for (var i = 0; i < aces; i++)
            if (value + 11 <= 21)
                value += 11;
            else
                value++;

        return value;
    }

    public static Embed ToEmbed(this string message, Color color = default)
    {
        return new EmbedBuilder()
            .WithDescription(message)
            .WithColor(color)
            .Build();
    }

    public static int GetConnectedUserCount(this IVoiceChannel channel)
    {
        if (channel.Guild is not SocketGuild guild)
            return 0;
        return guild.GetVoiceChannel(channel.Id) is not { } voiceChannel ? 0 : voiceChannel.ConnectedUsers.Count;
    }

    public static string TrimTo(this string str, int length)
    {
        return str.Length <= length ? str : str[..length];
    }
}