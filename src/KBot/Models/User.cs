// ReSharper disable UnusedAutoPropertyAccessor.Global

#pragma warning disable CS8618, MA0048, MA0016
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class User
{
    public User(SocketGuildUser user)
    {
        UserId = user.Id;
        GuildId = user.Guild.Id;
        UniqueId = user.Id + user.Guild.Id;
        Xp = 0;
        Level = 1;
        DailyXpClaim = DateTime.MinValue;
        Roles = new List<ulong>();
        Roles.AddRange(user.Roles.Where(x => !x.IsEveryone).Select(x => x.Id));
        Balance = 1000;
        DailyBalanceClaim = DateTime.MinValue;
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
        TransactionIds = new List<string>();
        WarnIds = new List<string>();
    }

    [BsonId]
    public ulong UniqueId { get; set; }

    [BsonElement("guild_id")]
    public ulong GuildId { get; set; }

    [BsonElement("user_id")]
    public ulong UserId { get; set; }

    [BsonElement("xp")]
    public int Xp { get; set; }

    [BsonIgnore]
    public int RequiredXp => (int)Math.Pow(Level * 4, 2);

    [BsonElement("level")]
    public int Level { get; set; }

    [BsonElement("osu_id")]
    public ulong OsuId { get; set; }

    [BsonElement("daily_xp_claim")]
    public DateTime DailyXpClaim { get; set; }

    [BsonElement("roles")]
    public List<ulong> Roles { get; set; }

    [BsonElement("balance")]
    public int Balance { get; set; }

    [BsonElement("daily_balance_claim")]
    public DateTime DailyBalanceClaim { get; set; }

    [BsonElement("wins")]
    public int Wins { get; set; }

    [BsonElement("losses")]
    public int Losses { get; set; }

    [BsonIgnore]
    public int GamesPlayed => Wins + Losses;

    [BsonIgnore]
    public int GambleLevel => GamesPlayed / 10;

    [BsonIgnore]
    public int MinimumBet
    {
        get
        {
            if (GambleLevel >= 100)
                return 1000000;
            return (int)Math.Round(Math.Pow(GambleLevel, 2.99996) + 185);
        }
    }

    [BsonElement("money_won")]
    public int MoneyWon { get; set; }

    [BsonElement("money_lost")]
    public int MoneyLost { get; set; }

    [BsonElement("transaction_ids")]
    public List<string> TransactionIds { get; set; }

    [BsonElement("game_result_ids")]
    public List<string> GameResultIds { get; set; }

    [BsonElement("warn_ids")]
    public List<string> WarnIds { get; set; }

    [BsonIgnore]
    public double WinRate => Math.Round(Wins / (double)GamesPlayed * 100, 2);

    [BsonIgnore]
    public int MoneyToBuyNextLevel => RequiredXp * 100;

    public EmbedBuilder ToEmbedBuilder(IUser user)
    {
        return new EmbedBuilder()
            .WithAuthor(user.Username, user.GetAvatarUrl())
            .WithColor(Color.Gold)
            .AddField("🆙 Level", $"`{GambleLevel.ToString(CultureInfo.InvariantCulture)}`")
            .AddField(
                "♦ Minimum Bet",
                $"`{MinimumBet.ToString("N0", CultureInfo.InvariantCulture)}`"
            )
            .AddField(
                "💳 Balance",
                $"`{Balance.ToString("N0", CultureInfo.InvariantCulture)}`",
                true
            )
            .AddField(
                "💰 Money Won",
                $"`{MoneyWon.ToString("N0", CultureInfo.InvariantCulture)}`",
                true
            )
            .AddField(
                "💸 Money Lost",
                $"`{MoneyLost.ToString("N0", CultureInfo.InvariantCulture)}`",
                true
            )
            .AddField("📈 Winrate", $"`{WinRate.ToString(CultureInfo.InvariantCulture)}%`", true)
            .AddField("🏆 Wins", $"`{Wins.ToString(CultureInfo.InvariantCulture)}`", true)
            .AddField("🚫 Loses", $"`{Losses.ToString(CultureInfo.InvariantCulture)}`", true);
    }
}
