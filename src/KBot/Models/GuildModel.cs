using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
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

    public GuildModel(ulong guildId, List<SocketGuildUser> users)
    {
        GuildId = guildId;
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
    public ulong UserId { get; set; }

    public int Points { get; set; }

    public int Level { get; set; }

    public GamblingProfile GamblingProfile { get; set; }
    public ulong OsuId { get; set; }

    public DateTime LastDailyClaim { get; set; }

    public DateTime LastVoiceChannelJoin { get; set; }

    public List<Warn> Warns { get; set; }

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

    public User(ulong userId)
    {
        UserId = userId;
        Points = 0;
        Level = 0;
        GamblingProfile = new GamblingProfile();
        OsuId = 0;
        Warns = new List<Warn>();
        LastDailyClaim = DateTime.MinValue;
        LastVoiceChannelJoin = DateTime.MinValue;
    }
}

public class GamblingProfile
{
    public int Money { get; set; }
    public DateTime LastDailyClaim { get; set; }
    public BlackJackProfile BlackJack { get; set; }
    public CoinFlipProfile CoinFlip { get; set; }
    public HighLowProfile HighLow { get; set; }
    
    public CrashProfile Crash { get; set; }

    [BsonIgnore]
    public int TotalPlayed => BlackJack.GamesPlayed + CoinFlip.GamesPlayed + HighLow.GamesPlayed + Crash.GamesPlayed;

    [BsonIgnore]
    public int TotalWon => BlackJack.Wins + CoinFlip.Wins + HighLow.Wins + Crash.Wins;

    [BsonIgnore]
    public int TotalLost => BlackJack.Losses + CoinFlip.Losses + HighLow.Losses + Crash.Losses;

    [BsonIgnore]
    public int TotalMoneyWon => BlackJack.MoneyWon + CoinFlip.MoneyWon + HighLow.MoneyWon + Crash.MoneyWon;

    [BsonIgnore]
    public int TotalMoneyLost => BlackJack.MoneyLost + CoinFlip.MoneyLost + HighLow.MoneyLost + Crash.MoneyLost;

    [BsonIgnore]
    public double TotalWinRate
    {
        get
        {
            var total = TotalWon;
            return Math.Round(total / (double)(TotalPlayed) * 100, 2);
        }
    }

    public GamblingProfile()
    {
        Money = 1000;
        BlackJack = new BlackJackProfile();
        CoinFlip = new CoinFlipProfile();
        HighLow = new HighLowProfile();
        Crash = new CrashProfile();
    }

    public override string ToString()
    {
        var s = "";
        s += $"Elérhető egyenleg: **{Money}**\n";
        s += $"Összes győzelem: **{TotalWon}**\n";
        s += $"Összes vereség: **{TotalLost}**\n";
        s += $"Összes győzelmi ráta: **{TotalWinRate}%**\n";
        s += $"Összes nyert pénz: **{TotalMoneyWon}**\n";
        s += $"Összes vesztett pénz: **{TotalMoneyLost}**";
        return s;
    }

    public EmbedBuilder ToEmbedBuilder()
    {
        return new EmbedBuilder()
            .WithTitle("Szerencsejáték profil")
            .WithColor(Color.Gold)
            .AddField("💳 Egyenleg", $"`{Money.ToString()} 🪙KCoin`", true)
            .AddField("🏆 Győzelmek", $"`{TotalWon.ToString()}`", true)
            .AddField("🚫 Vereségek", $"`{TotalLost.ToString()}`", true)
            .AddField("📈 Győzelmi ráta", $"`{TotalWinRate.ToString()}% ({TotalWon.ToString()}W/{TotalLost.ToString()}L)`", true)
            .AddField("💰 Nyert pénz", $"`{TotalMoneyWon.ToString()} 🪙KCoin`", true)
            .AddField("💸 Vesztett pénz", $"`{TotalMoneyLost.ToString()} 🪙KCoin`", true);
    }
}

public class CrashProfile
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MoneyWon { get; set; }
    public int MoneyLost { get; set; }

    [BsonIgnore]
    public int GamesPlayed => Wins + Losses;

    [BsonIgnore]
    public double WinRate
    {
        get
        {
            var total = Wins;
            return Math.Round(total / (double)GamesPlayed * 100, 2);
        }
    }

    public CrashProfile()
    {
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
    }

    public override string ToString()
    {
        var s = "";
        s += $"Győzelmek: **{Wins}**\n";
        s += $"Vereségek: **{Losses}**\n";
        s += $"Győzelem ráta: **{WinRate}%**\n";
        s += $"Nyert pénz: **{MoneyWon}**\n";
        s += $"Vesztett pénz: **{MoneyLost}**";
        return s;
    }

    public EmbedBuilder ToEmbedBuilder()
    {
        return new EmbedBuilder()
            .WithTitle("Crash statisztikák")
            .WithColor(Color.Gold)
            .AddField("💰 Nyert pénz", $"`{MoneyWon.ToString()} 🪙KCoin`", true)
            .AddField("💸 Vesztett pénz", $"`{MoneyLost.ToString()} 🪙KCoin`", true)
            .AddField("🏆 Győzelmek", $"`{Wins.ToString()}`", true)
            .AddField("🚫 Vereségek", $"`{Losses.ToString()}`", true)
            .AddField("📈 Győzelmi ráta", $"`{WinRate.ToString()}% ({Wins.ToString()}W/{Losses.ToString()}L)`", true);
    }
}

public class HighLowProfile
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MoneyWon { get; set; }
    public int MoneyLost { get; set; }

    [BsonIgnore]
    public int GamesPlayed => Wins + Losses;

    [BsonIgnore]
    public double WinRate
    {
        get
        {
            var total = Wins;
            return Math.Round(total / (double)GamesPlayed * 100, 2);
        }
    }

    public HighLowProfile()
    {
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
    }

    public override string ToString()
    {
        var s = "";
        s += $"Győzelmek: **{Wins}**\n";
        s += $"Vereségek: **{Losses}**\n";
        s += $"Győzelem ráta: **{WinRate}%**\n";
        s += $"Nyert pénz: **{MoneyWon}**\n";
        s += $"Vesztett pénz: **{MoneyLost}**";
        return s;
    }

    public EmbedBuilder ToEmbedBuilder()
    {
        return new EmbedBuilder()
            .WithTitle("High/Low profil")
            .WithColor(Color.Gold)
            .AddField("💰 Nyert pénz", $"`{MoneyWon.ToString()} 🪙KCoin`", true)
            .AddField("💸 Vesztett pénz", $"`{MoneyLost.ToString()} 🪙KCoin`", true)
            .AddField("🏆 Győzelmek", $"`{Wins.ToString()}`", true)
            .AddField("🚫 Vereségek", $"`{Losses.ToString()}`", true)
            .AddField("📈 Győzelmi ráta", $"`{WinRate.ToString()}% ({Wins.ToString()}W/{Losses.ToString()}L)`", true);
    }
}

public class CoinFlipProfile
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MoneyWon { get; set; }
    public int MoneyLost { get; set; }

    [BsonIgnore]
    public int GamesPlayed => Wins + Losses;

    [BsonIgnore]
    public double WinRate
    {
        get
        {
            var total = Wins;
            return Math.Round(total / (double)GamesPlayed * 100, 2);
        }
    }

    public CoinFlipProfile()
    {
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
    }

    public override string ToString()
    {
        var s = "";
        s += $"Győzelmek: **{Wins}**\n";
        s += $"Vereségek: **{Losses}**\n";
        s += $"Győzelem ráta: **{WinRate}%**\n";
        s += $"Nyert pénz: **{MoneyWon}**\n";
        s += $"Vesztett pénz: **{MoneyLost}**";
        return s;
    }

    public EmbedBuilder ToEmbedBuilder()
    {
        return new EmbedBuilder()
            .WithTitle("CoinFlip profil")
            .WithColor(Color.Gold)
            .AddField("💰 Nyert pénz", $"`{MoneyWon.ToString()} 🪙KCoin`", true)
            .AddField("💸 Vesztett pénz", $"`{MoneyLost.ToString()} 🪙KCoin`", true)
            .AddField("🏆 Győzelmek", $"`{Wins.ToString()}`", true)
            .AddField("🚫 Vereségek", $"`{Losses.ToString()}`", true)
            .AddField("📈 Győzelmi ráta", $"`{WinRate.ToString()}% ({Wins.ToString()}W/{Losses.ToString()}L)`", true);
    }
}

public class BlackJackProfile
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MoneyWon { get; set; }
    public int MoneyLost { get; set; }

    [BsonIgnore]
    public int GamesPlayed => Wins + Losses;

    [BsonIgnore]
    public double WinRate
    {
        get
        {
            var total = Wins;
            return Math.Round(total / (double)GamesPlayed * 100, 2);
        }
    }

    public BlackJackProfile()
    {
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
    }

    public override string ToString()
    {
        var s = "";
        s += $"Győzelmek: **{Wins}**\n";
        s += $"Vereségek: **{Losses}**\n";
        s += $"Győzelem ráta: **{WinRate}%**\n";
        s += $"Nyert pénz: **{MoneyWon}**\n";
        s += $"Vesztett pénz: **{ MoneyLost}**";
        return s;
    }

    public EmbedBuilder ToEmbedBuilder()
    {
        return new EmbedBuilder()
            .WithTitle("BlackJack profil")
            .WithColor(Color.Gold)
            .AddField("💰 Nyert pénz", $"`{MoneyWon.ToString()} 🪙KCoin`", true)
            .AddField("💸 Vesztett pénz", $"`{MoneyLost.ToString()} 🪙KCoin`", true)
            .AddField("🏆 Győzelmek", $"`{Wins.ToString()}`", true)
            .AddField("🚫 Vereségek", $"`{Losses.ToString()}`", true)
            .AddField("📈 Győzelmi ráta", $"`{WinRate.ToString()}% ({Wins.ToString()}W/{Losses.ToString()}L)`", true);
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
    public int PointsToLevelUp { get; set; }

    public ulong AnnouncementChannelId { get; set; }

    public ulong AfkChannelId { get; set; }

    public List<LevelRole> LevelRoles { get; set; }

    public Leveling()
    {
        Enabled = false;
        PointsToLevelUp = 0;
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
