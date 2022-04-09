using System;
using System.Collections.Generic;
using System.Linq;
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
    public string DocId { get; set; }

    [BsonElement("guildid")] public ulong Id { get; private set; }

    [BsonElement("config")] public GuildConfig Config { get; private set; }

    [BsonElement("users")] public List<User> Users { get; private set; }
    
    [BsonElement("buttonroles")] public List<ButtonRoleMessage> ButtonRoles { get; private set; }

    public GuildModel(List<SocketGuildUser> users)
    {
        Id = users[0].Guild.Id;
        Config = new GuildConfig();
        Users = new List<User>();
        ButtonRoles = new List<ButtonRoleMessage>();
        users.ConvertAll(x => new User(x)).ForEach(x => Users.Add(x));
    }
    
    public void AddUser(SocketGuildUser user)
    {
        Users.Add(new User(user));
    }
    
    public void ReplaceConfig(GuildConfig config)
    {
        Config = config;
    }
}

public class ButtonRoleMessage
{
    public ButtonRoleMessage(ulong channelId, ulong messageId, string title, string description)
    {
        ChannelId = channelId;
        MessageId = messageId;
        Title = title;
        Description = description;
        Roles = new List<ButtonRole>();
    }

    public bool AddRole(ButtonRole role)
    {
        if (Roles.Exists(x => x.RoleId == role.RoleId))
            return false;
        Roles.Add(role);
        return true;
    }
    
    public bool RemoveRole(IRole role)
    {
        if (!Roles.Exists(x => x.RoleId == role.Id))
            return false;
        Roles.Remove(Roles.Find(x => x.RoleId == role.Id));
        return true;
    }
    public MessageComponent GetButtons()
    {
        var comp = new ComponentBuilder();
        foreach (var role in Roles)
        {
            var emoteResult = Emote.TryParse(role.Emote, out var emote);
            var emojiResult = Emoji.TryParse(role.Emote, out var emoji);
            if (emoteResult)
                comp.WithButton(role.Title, $"rrtr:{role.RoleId}", emote: emote);
            else if (emojiResult)
                comp.WithButton(role.Title, $"rrtr:{role.RoleId}", emote: emoji);
            else
                comp.WithButton(role.Title, $"rrtr:{role.RoleId}");
        }
        return comp.Build();
    }
    
    
    [BsonElement("title")] public string Title { get; set; }
    [BsonElement("description")] public string Description { get; set; }
    [BsonElement("roles")] public List<ButtonRole> Roles { get; private set; }
    [BsonElement("messageid")] public ulong MessageId { get; private set; }
    [BsonElement("channelid")] public ulong ChannelId { get; private set; }
}

public class ButtonRole
{
    public ButtonRole(ulong roleId, string title, string emote)
    {
        RoleId = roleId;
        Title = title;
        Emote = emote;
    }
    
    [BsonElement("emote")] public string Emote { get; private set; }
    [BsonElement("title")] public string Title { get; private set; }
    [BsonElement("roleid")] public ulong RoleId { get; private set; }
}

public class GuildConfig
{
    [BsonElement("announcements")] public AnnouncementConfig Announcements { get; set; }

    [BsonElement("temporaryvoice")] public TemporaryChannels TemporaryVoice { get; set; }

    [BsonElement("movieevents")] public MovieEvents MovieEvents { get; set; }

    [BsonElement("tourevents")] public TourEvents TourEvents { get; set; }

    [BsonElement("leveling")] public Leveling Leveling { get; set; }
    [BsonElement("suggestions")] public Suggestions Suggestions { get; set; }

    public GuildConfig()
    {
        Announcements = new AnnouncementConfig();
        TemporaryVoice = new TemporaryChannels();
        MovieEvents = new MovieEvents();
        TourEvents = new TourEvents();
        Leveling = new Leveling();
        Suggestions = new Suggestions();
    }
}

public class User
{
    [BsonElement("userid")] public ulong Id { get; private set; }
    [BsonElement("xp")] public int XP { get; set; }

    [BsonElement("level")] public int Level { get; set; }

    [BsonElement("gambling")] public GamblingProfile Gambling { get; private set; }
    [BsonElement("osuid")] public ulong OsuId { get; set; }

    [BsonElement("dailyclaimdate")] public DateTime DailyClaimDate { get; set; }

    [BsonElement("voiceactivitydate")] public DateTime LastVoiceActivityDate { get; set; }

    [BsonElement("warns")] public List<Warn> Warns { get; private set; }
    [BsonElement("roles")] public List<ulong> Roles { get; private set; }
    [BsonElement("transactions")] public List<Transaction> Transactions { get; private set; }
    [BsonElement("boughtchannels")] public List<DiscordChannel> BoughtChannels { get; private set; }

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

    [BsonIgnore] public int RequiredXp => (int)Math.Pow(Level * 4, 2);

    [BsonIgnore]
    public int Money
    {
        get => Gambling.Balance;
        set => Gambling.Balance = value;
    }

    public int MoneyToBuyLevel(int level)
    {
        var tlevel = Level;
        var total = 0;
        for (var i = 0; i < level; i++)
        {
            total += (int)Math.Pow((Level + i) * 4, 2);
        }
        return (int)Math.Round((decimal)(total * 2) - XP);
    }

    public User(SocketGuildUser user)
    {
        Id = user.Id;
        XP = 0;
        Level = 0;
        Gambling = new GamblingProfile();
        OsuId = 0;
        Warns = new List<Warn>();
        Roles = new List<ulong>();
        user.Roles.Select(x=> x.Id).ToList().ForEach(x => Roles.Add(x));
        BoughtChannels = new List<DiscordChannel>();
        Transactions = new List<Transaction>();
        DailyClaimDate = DateTime.MinValue;
        LastVoiceActivityDate = DateTime.MinValue;
    }    
    public User(SocketUser user, List<ulong> roles)
    {
        Id = user.Id;
        XP = 0;
        Level = 0;
        Gambling = new GamblingProfile();
        OsuId = 0;
        Warns = new List<Warn>();
        Roles = new List<ulong>();
        Roles.AddRange(roles);
        BoughtChannels = new List<DiscordChannel>();
        Transactions = new List<Transaction>();
        DailyClaimDate = DateTime.MinValue;
        LastVoiceActivityDate = DateTime.MinValue;
    }
}

public class DiscordChannel
{
    public DiscordChannel(ulong channelId, DiscordChannelType channelType)
    {
        Id = channelId;
        Type = channelType;
    }

    [BsonElement("type")] public DiscordChannelType Type { get; set; }
    [BsonElement("channelid")] public ulong Id { get; set; }
}

public class GamblingProfile
{
    [BsonElement("balance")] public int Balance { get; set; }
    [BsonElement("dailyclaimdate")] public DateTime DailyClaimDate { get; set; }
    [BsonIgnore] public int GamesPlayed => Wins + Losses;
    [BsonElement("wins")] public int Wins { get; set; }
    [BsonElement("losses")] public int Losses { get; set; }
    [BsonElement("moneywon")] public int MoneyWon { get; set; }
    [BsonElement("moneylost")] public int MoneyLost { get; set; }

    [BsonIgnore]
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
        Balance = 1000;
        DailyClaimDate = DateTime.MinValue;
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
    }

    public EmbedBuilder ToEmbedBuilder(IUser user)
    {
        return new EmbedBuilder()
            .WithAuthor(user.Username, user.GetAvatarUrl())
            .WithColor(Color.Gold)
            .AddField("💳 Egyenleg", $"`{Balance.ToString()}`", true)
            .AddField("💰 Nyereség", $"`{MoneyWon.ToString()}`", true)
            .AddField("💸 Veszteség", $"`{MoneyLost.ToString()}`", true)
            .AddField("📈 Győzelmi ráta", $"`{WinRate.ToString()}%`", true)
            .AddField("🏆 Győzelmek", $"`{Wins.ToString()}`", true)
            .AddField("🚫 Vereségek", $"`{Losses.ToString()}`", true);
    }
}

public class Transaction
{
    [BsonElement("id")] public string Id { get; set; }
    [BsonElement("type")] public TransactionType Type { get; set; }
    [BsonElement("amount")] public int Amount { get; set; }
    [BsonElement("date")] public DateTime Date { get; set; }
    [BsonElement("desc")] public string Description { get; set; }
    
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

public class AnnouncementConfig
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("joinchannelid")] public ulong JoinChannelId { get; set; }
    [BsonElement("joinroleid")] public ulong JoinRoleId { get; set; }
    [BsonElement("leftchannelid")] public ulong LeftChannelId { get; set; }
    [BsonElement("banchannelid")] public ulong BanChannelId { get; set; }
    [BsonElement("unbanchannelid")] public ulong UnbanChannelId { get; set; }

    public AnnouncementConfig()
    {
        Enabled = false;
        JoinChannelId = 0;
        JoinRoleId = 0;
        LeftChannelId = 0;
        BanChannelId = 0;
        UnbanChannelId = 0;
    }
}

public class TemporaryChannels
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("categoryid")] public ulong CategoryId { get; set; }

    [BsonElement("createchannelid")] public ulong CreateChannelId { get; set; }

    public TemporaryChannels()
    {
        Enabled = false;
        CategoryId = 0;
        CreateChannelId = 0;
    }
}

public class MovieEvents
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("announcechannelid")]public ulong AnnounceChannelId { get; set; }

    [BsonElement("streamchannelid")] public ulong StreamChannelId { get; set; }

    [BsonElement("roleid")] public ulong RoleId { get; set; }

    public MovieEvents()
    {
        Enabled = false;
        AnnounceChannelId = 0;
        StreamChannelId = 0;
        RoleId = 0;
    }
}

public class TourEvents
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("announcechannelid")] public ulong AnnounceChannelId { get; set; }
    [BsonElement("roleid")] public ulong RoleId { get; set; }

    public TourEvents()
    {
        Enabled = false;
        AnnounceChannelId = 0;
        RoleId = 0;
    }
}

public class Leveling
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("announcechannelid")] public ulong AnnounceChannelId { get; set; }
    [BsonElement("afkchannelid")] public ulong AfkChannelId { get; set; }
    [BsonElement("levelroles")] public List<LevelRole> LevelRoles { get; set; }

    public Leveling()
    {
        Enabled = false;
        AnnounceChannelId = 0;
        AfkChannelId = 0;
        LevelRoles = new List<LevelRole>();
    }
}

public class Suggestions
{
    [BsonElement("enabled")] public bool Enabled { get; set; }
    [BsonElement("announcechannelid")] public ulong AnnounceChannelId { get; set; }

    public Suggestions()
    {
        Enabled = false;
        AnnounceChannelId = 0;
    }
}

public class LevelRole
{
    public LevelRole(IRole role, int level)
    {
        Level = level;
        Id = role.Id;
    }
    [BsonElement("roleid")] public ulong Id { get; }
    [BsonElement("level")] public int Level { get; }
    
}
