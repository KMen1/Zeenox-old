using System;
using System.Collections.Generic;
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
        DocId = user.Id + user.Guild.Id;
        Xp = 0;
        Level = 1;
        DailyXpClaim = DateTime.MinValue;
        VoiceChannelJoin = DateTime.MinValue;
        Roles = new List<ulong>();
        Roles.AddRange(user.Roles.Select(x => x.Id));
        Balance = 10000;
        DailyBalanceClaim = DateTime.MinValue;
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
        TransactionIds = new List<string>();
    }

    [BsonId] public ulong DocId { get; set; }
    [BsonElement("guild_id")] public ulong GuildId { get; set; }
    [BsonElement("user_id")] public ulong UserId { get; set; }

    [BsonElement("xp")] public int Xp { get; set; }
    [BsonIgnore] public int RequiredXp => (int) Math.Pow(Level * 4, 2);
    [BsonIgnore] public int TotalXp
    {
        get
        {
            var total = 0;
            for (var i = 0; i < Level; i++) total += (int) Math.Pow(i * 4, 2);
            return total + Xp;
        }
    }
    public int MoneyToBuyLevel(int level)
    {
        var total = 0;
        for (var i = 0; i < level; i++) total += (int) Math.Pow((Level + i) * 4, 2);
        return (int) Math.Round((decimal) (total * 2) - Xp);
    }

    [BsonElement("level")] public int Level { get; set; }
    [BsonElement("osu_id")] public ulong OsuId { get; set; }
    [BsonElement("daily_xp_claim")] public DateTime DailyXpClaim { get; set; }
    [BsonElement("voice_channel_join")] public DateTime VoiceChannelJoin { get; set; }
    [BsonElement("roles")] public List<ulong> Roles { get; set; }

    [BsonElement("balance")] public int Balance { get; set; }
    [BsonElement("daily_balance_claim")] public DateTime DailyBalanceClaim { get; set; }
    [BsonElement("wins")] public int Wins { get; set; }
    [BsonElement("losses")] public int Losses { get; set; }
    [BsonIgnore] public int GamesPlayed => Wins + Losses;
    [BsonElement("money_won")] public int MoneyWon { get; set; }
    [BsonElement("money_lost")] public int MoneyLost { get; set; }
    [BsonElement("transaction_ids")] public List<string> TransactionIds { get; set; }
    [BsonElement("game_result_ids")] public List<string> GameResultIds { get; set; }
    [BsonIgnore] public double WinRate => Math.Round(Wins / (double) GamesPlayed * 100, 2);
    
    public EmbedBuilder ToEmbedBuilder(IUser user)
    {
        return new EmbedBuilder()
            .WithAuthor(user.Username, user.GetAvatarUrl())
            .WithColor(Color.Gold)
            .AddField("💳 Balance", $"`{Balance.ToString()}`", true)
            .AddField("💰 Money Won", $"`{MoneyWon.ToString()}`", true)
            .AddField("💸 Money Lost", $"`{MoneyLost.ToString()}`", true)
            .AddField("📈 Winrate", $"`{WinRate.ToString()}%`", true)
            .AddField("🏆 Wins", $"`{Wins.ToString()}`", true)
            .AddField("🚫 Loses", $"`{Losses.ToString()}`", true);
    }
}