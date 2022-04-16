using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models.User;

public class User
{
    public User(SocketGuildUser user)
    {
        Id = user.Id;
        Xp = 0;
        Level = 1;
        Gambling = new Gambling();
        OsuId = 0;
        Warns = new List<Warn>();
        Roles = new List<ulong>();
        user.Roles.Select(x => x.Id).ToList().ForEach(x => Roles.Add(x));
        Transactions = new List<Transaction>();
        DailyClaimDate = null;
        LastVoiceActivityDate = null;
    }

    public User(SocketUser user, IEnumerable<ulong> roles)
    {
        Id = user.Id;
        Xp = 0;
        Level = 1;
        Gambling = new Gambling();
        OsuId = 0;
        Warns = new List<Warn>();
        Roles = new List<ulong>();
        Roles.AddRange(roles);
        Transactions = new List<Transaction>();
        DailyClaimDate = DateTime.MinValue;
        LastVoiceActivityDate = DateTime.MinValue;
    }

    [BsonElement("userid")] public ulong Id { get; set; }
    [BsonElement("xp")] public int Xp { get; set; }
    [BsonIgnore] public int XpNeeded => (int) Math.Pow(Level * 4, 2);
    [BsonElement("level")] public int Level { get; set; }
    [BsonElement("osuid")] public ulong OsuId { get; set; }
    [BsonElement("dailyclaimdate")] public DateTime? DailyClaimDate { get; set; }
    [BsonElement("voiceactivitydate")] public DateTime? LastVoiceActivityDate { get; set; }
    [BsonElement("gambling")] public Gambling Gambling { get; set; }
    [BsonElement("warns")] public List<Warn> Warns { get; set; }
    [BsonElement("roles")] public List<ulong> Roles { get; set; }
    [BsonElement("transactions")] public List<Transaction> Transactions { get; set; }

    [BsonIgnore]
    public int Money
    {
        get => Gambling.Balance;
        set => Gambling.Balance = value;
    }

    [BsonIgnore]
    public int TotalXp
    {
        get
        {
            var total = 0;
            for (var i = 0; i < Level; i++) total += (int) Math.Pow(i * 4, 2);
            return total;
        }
    }

    public int MoneyToBuyLevel(int level)
    {
        var total = 0;
        for (var i = 0; i < level; i++) total += (int) Math.Pow((Level + i) * 4, 2);
        return (int) Math.Round((decimal) (total * 2) - Xp);
    }
}

public class Gambling
{
    public Gambling()
    {
        Balance = 10000;
        DailyClaimDate = null;
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
    }

    [BsonElement("balance")] public int Balance { get; set; }
    [BsonElement("dailyclaimdate")] public DateTime? DailyClaimDate { get; set; }
    [BsonIgnore] public int GamesPlayed => Wins + Losses;
    [BsonElement("wins")] public int Wins { get; set; }
    [BsonElement("losses")] public int Losses { get; set; }
    [BsonElement("moneywon")] public int MoneyWon { get; set; }
    [BsonElement("moneylost")] public int MoneyLost { get; set; }

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

public class Transaction
{
    public Transaction(string id, TransactionType type, int amount, string description = "")
    {
        Id = id;
        Type = type;
        Amount = amount;
        Description = description;
        Date = DateTime.UtcNow;
    }

    [BsonElement("id")] public string Id { get; set; }
    [BsonElement("type")] public TransactionType Type { get; set; }
    [BsonElement("amount")] public int Amount { get; set; }
    [BsonElement("date")] public DateTime Date { get; set; }
    [BsonElement("desc")] public string Description { get; set; }

    public override string ToString()
    {
        return $"`ID: {Id}` `Date: {Date.ToString("yyyy.MM.dd")}` `Amount: {Amount}`";
    }
}

public class Warn
{
    public Warn(ulong moderatorId, string reason, DateTime date)
    {
        ModeratorId = moderatorId;
        Reason = reason;
        Date = date;
    }

    [BsonElement("moderatorid")] public ulong ModeratorId { get; }
    [BsonElement("reason")] public string Reason { get; }
    [BsonElement("date")] public DateTime Date { get; }
}