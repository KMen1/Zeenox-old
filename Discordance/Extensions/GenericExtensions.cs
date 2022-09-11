using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Discord;
using Discord.WebSocket;
using Discordance.Models;
using Discordance.Modules.Gambling;
using SkiaSharp;
using Color = Discord.Color;
using Game = Discordance.Models.Game;

namespace Discordance.Extensions;

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
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }

    public static Embed ToEmbed(this IEnumerable<Perk> perks)
    {
        var eb = new EmbedBuilder()
            .WithTitle("Shrine of Secrets")
            .WithColor(Color.DarkOrange)
            .WithDescription(
                $"🏁 <t:{DateTimeOffset.Now.GetNextWeekday(DayOfWeek.Thursday).ToUnixTimeSeconds()}:R>"
            );
        //foreach (var perk in perks)
        //eb.AddField(perk.Name, $"from {perk.CharacterName}", true);
        return eb.Build();
    }

    public static Embed[] ToEmbedArray(this IEnumerable<Game> games)
    {
        var date = ((DateTimeOffset)DateTime.Today)
            .GetNextWeekday(DayOfWeek.Thursday)
            .AddHours(17)
            .ToUnixTimeSeconds();
        return games
            .Select(
                game =>
                    new EmbedBuilder()
                        .WithTitle(game.Title)
                        .WithDescription(
                            $"`{game.Description}`\n\n"
                                + $"💰 **{game.Price.TotalPrice.FmtPrice.OriginalPrice} -> Free** \n\n"
                                + $"🏁 <t:{date}:R>\n\n"
                                + $"[Open in browser]({game.EpicUrl})"
                        )
                        .WithImageUrl(game.KeyImages[0].Url.ToString())
                        .WithColor(Color.Gold)
                        .Build()
            )
            .ToArray();
    }

    public static bool CanAffectGame(this IGame? game, ulong userId, out Embed? embed)
    {
        if (game is null)
        {
            embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Game not found!**")
                .Build();
            return false;
        }

        if (game.UserId != userId)
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

    public static string ParseMessage(string message, IUser user, IGuild guild)
    {
        message = message.Replace("{user.name}", user.Username, StringComparison.OrdinalIgnoreCase);
        message = message.Replace(
            "{user.discriminator}",
            user.Discriminator,
            StringComparison.OrdinalIgnoreCase
        );
        message = message.Replace("{guild.name}", guild.Name, StringComparison.OrdinalIgnoreCase);
        return message;
    }

    public static string ToTimeString(this TimeSpan timeSpan)
    {
        var sb = new StringBuilder();
        if (timeSpan.TotalSeconds < 60)
            return $"00:{timeSpan.Seconds.ToString("00")}";
        if (timeSpan.Days > 0)
            sb.Append($"{timeSpan.Days}:");
        if (timeSpan.Hours > 0)
            sb.Append($"{timeSpan.Hours.ToString("00")}:");
        if (timeSpan.Minutes > 0)
            sb.Append($"{timeSpan.Minutes.ToString("00")}:");
        sb.Append(timeSpan.Seconds.ToString("00"));
        return sb.ToString();
    }
}
