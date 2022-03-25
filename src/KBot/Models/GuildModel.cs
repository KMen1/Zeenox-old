using System;
using System.Collections.Generic;
using System.ComponentModel;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class GuildModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public ulong GuildId { get; set; }

    public GuildConfig Config { get; set; }

    public List<User> Users { get; set; }

    public GuildModel(List<SocketGuildUser> users)
    {
        GuildId = users[0].Guild.Id;
        Config = new GuildConfig();
        Users = new List<User>();
        foreach (var user in users)
        {
            Users.Add(new User(user.Id));
        }
    }
}

public class GuildConfig
{
    public AnnouncementConfig Announcements { get; set; }

    public TemporaryChannels TemporaryChannels { get; set; }

    public MovieEvents MovieEvents { get; set; }

    public TourEvents TourEvents { get; set; }

    public Leveling Leveling { get; set; }
    public Suggestions Suggestions { get; set; }

    public GuildConfig()
    {
        Announcements = new AnnouncementConfig();
        TemporaryChannels = new TemporaryChannels();
        MovieEvents = new MovieEvents();
        TourEvents = new TourEvents();
        Leveling = new Leveling();
        Suggestions = new Suggestions();
    }
}

public class Suggestions
{
    public bool Enabled { get; set; }
    public ulong AnnouncementChannelId { get; set; }

    public Suggestions()
    {
        Enabled = false;
        AnnouncementChannelId = 0;
    }
}

public class User
{
    [BsonElement("UserId")]
    public ulong Id { get; set; }
    [BsonElement("Points")]
    public int XP { get; set; }

    public int Level { get; set; }

    [BsonElement("GamblingProfile")]
    public GamblingProfile Gambling { get; set; }
    public ulong OsuId { get; set; }

    public DateTime LastDailyClaim { get; set; }

    public DateTime LastVoiceChannelJoin { get; set; }

    public List<Warn> Warns { get; set; }
    public List<ulong> Roles { get; set; }
    public List<Transaction> Transactions { get; set; }
    public List<DiscordChannel> BoughtChannels { get; set; }
    public List<ulong> BoughtRoles { get; set; }

    [BsonIgnore]
    public int TotalXp
    {
        get
        {
            var total = 0;
            for (var i = 0; i < Level; i++)
            {
                total += (int)Math.Pow(i * 4, 2);
            }
            return total;
        }
    }

    [BsonIgnore]
    public int RequiredXp => (int)Math.Pow(Level * 4, 2);

    [BsonIgnore]
    public int Money
    {
        get => Gambling.Money;
        set => Gambling.Money = value;
    }

    public int MoneyToBuyLevel(int level)
    {
        var tlevel = Level;
        var total = 0;
        for (int i = 0; i < level; i++)
        {
            total += (int)Math.Pow((Level + i) * 4, 2);
        }
        return (int)Math.Round((decimal)(total * 2) - XP);
    }

    public User(ulong userId)
    {
        Id = userId;
        XP = 0;
        Level = 0;
        Gambling = new GamblingProfile();
        OsuId = 0;
        Warns = new List<Warn>();
        Roles = new List<ulong>();
        BoughtChannels = new List<DiscordChannel>();
        BoughtRoles = new List<ulong>();
        Transactions = new List<Transaction>();
        LastDailyClaim = DateTime.MinValue;
        LastVoiceChannelJoin = DateTime.MinValue;
    }
}

public class DiscordChannel
{
    public DiscordChannel(ulong channelId, DiscordChannelType channelType)
    {
        ChannelId = channelId;
        ChannelType = channelType;
    }

    public DiscordChannelType ChannelType { get; set; }
    public ulong ChannelId { get; set; }
}

public enum DiscordChannelType
{
    Voice,
    Text,
    Category
}

public class GamblingProfile
{
    public int Money { get; set; }
    public DateTime LastDailyClaim { get; set; }
    public int GamesPlayed => Wins + Losses;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MoneyWon { get; set; }
    public int MoneyLost { get; set; }

    public double WinRate
    {
        get
        {
            var total = Wins;
            return Math.Round(total / (double)(GamesPlayed) * 100, 2);
        }
    }

    public GamblingProfile()
    {
        Money = 1000;
        LastDailyClaim = DateTime.MinValue;
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
    }

    public EmbedBuilder ToEmbedBuilder()
    {
        return new EmbedBuilder()
            .WithTitle("Szerencsejáték profil")
            .WithColor(Color.Gold)
            .AddField("💳 Egyenleg", $"`{Money.ToString()} 🪙KCoin`", true)
            .AddField("🏆 Győzelmek", $"`{Wins.ToString()}`", true)
            .AddField("🚫 Vereségek", $"`{Losses.ToString()}`", true)
            .AddField("📈 Győzelmi ráta", $"`{WinRate.ToString()}% ({Wins.ToString()}W/{Losses.ToString()}L)`", true)
            .AddField("💰 Nyert pénz", $"`{MoneyWon.ToString()} 🪙KCoin`", true)
            .AddField("💸 Vesztett pénz", $"`{MoneyLost.ToString()} 🪙KCoin`", true);
    }
}

public class Transaction
{
    public string Id { get; set; }
    public TransactionType Type { get; set; }
    public int Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    
    public Transaction(string id, TransactionType type, int amount, string description = "")
    {
        Id = id;
        Type = type;
        Amount = amount;
        Description = description;
        Date = DateTime.UtcNow;
    }

    public override string ToString()
    {
        return $"ID: `{Id}` ({Date.ToString("yyyy.MM.dd")}): **{Type.GetDescription()}** ({Amount.ToString()} KCoin)" + (string.IsNullOrEmpty(Description) ? "" : $" - {Description}");
    }
}

public enum TransactionType
{
    [Description("Ismeretlen")]
    Unknown,
    [Description("Korrekció")]
    Correction,
    [Description("Szerencsejáték")]
    Gambling,
    [Description("Utalás küldés")]
    TransferSend,
    [Description("Utalás fogadás")]
    TransferReceive,
    [Description("Napi begyűjtés")]
    DailyClaim,
    [Description("Vásárlás")]
    ShopPurchase,
}

public class Warn
{
    public Warn(ulong moderatorId, string reason, DateTime date)
    {
        ModeratorId = moderatorId;
        Reason = reason;
        Date = date;
    }
    public ulong ModeratorId { get; }

    public string Reason { get; }

    public DateTime Date { get; }
}

public class AnnouncementConfig
{
    public bool Enabled { get; set; }
    public ulong UserJoinedChannelId { get; set; }
    public ulong JoinRoleId { get; set; }
    public ulong UserLeftChannelId { get; set; }
    public ulong UserBannedChannelId { get; set; }
    public ulong UserUnbannedChannelId { get; set; }

    public AnnouncementConfig()
    {
        Enabled = false;
        UserJoinedChannelId = 0;
        JoinRoleId = 0;
        UserLeftChannelId = 0;
        UserBannedChannelId = 0;
        UserUnbannedChannelId = 0;
    }
}

public class TemporaryChannels
{
    public bool Enabled { get; set; }
    public ulong CategoryId { get; set; }

    public ulong CreateChannelId { get; set; }

    public TemporaryChannels()
    {
        Enabled = false;
        CategoryId = 0;
        CreateChannelId = 0;
    }
}

public class MovieEvents
{
    public bool Enabled { get; set; }
    public ulong AnnouncementChannelId { get; set; }

    public ulong StreamingChannelId { get; set; }

    public ulong RoleId { get; set; }

    public MovieEvents()
    {
        Enabled = false;
        AnnouncementChannelId = 0;
        StreamingChannelId = 0;
        RoleId = 0;
    }
}

public class TourEvents
{
    public bool Enabled { get; set; }
    public ulong AnnouncementChannelId { get; set; }

    public ulong RoleId { get; set; }

    public TourEvents()
    {
        Enabled = false;
        AnnouncementChannelId = 0;
        RoleId = 0;
    }
}

public class Leveling
{
    public bool Enabled { get; set; }
    public ulong AnnouncementChannelId { get; set; }

    public ulong AfkChannelId { get; set; }

    public List<LevelRole> LevelRoles { get; set; }

    public Leveling()
    {
        Enabled = false;
        AnnouncementChannelId = 0;
        AfkChannelId = 0;
        LevelRoles = new List<LevelRole>();
    }
}

public class LevelRole
{
    public LevelRole(int level, ulong roleId)
    {
        Level = level;
        RoleId = roleId;
    }
    public int Level { get; }
    public ulong RoleId { get; }
}
