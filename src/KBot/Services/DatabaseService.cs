using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using KBot.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KBot.Services;

public class DatabaseService : DiscordClientService
{
    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildModel> _collection;

    public DatabaseService(DiscordSocketClient client, ILogger<DatabaseService> logger, BotConfig config, IMemoryCache cache, IMongoDatabase database) : base(client, logger)
    {
        _client = client;
        _cache = cache;
        _collection = database.GetCollection<GuildModel>(config.MongoDb.Collection);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.JoinedGuild += RegisterNewGuildAsync;
        return Task.CompletedTask;
    }

    private async Task RegisterNewGuildAsync(SocketGuild guild)
    {
        var isGuildInDb = await (await _collection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false))
            .AnyAsync().ConfigureAwait(false);
        if (!isGuildInDb)
        {
            await RegisterGuildAsync(guild.Id).ConfigureAwait(false);
        }
    }

    private async Task<GuildModel> RegisterGuildAsync(ulong guildId)
    {
        var guild = new GuildModel
        {
            GuildId = guildId,
            Config = new GuildConfig
            {
                Announcements = new AnnouncementConfig
                {
                    Enabled = false,
                    JoinRoleId = 0,
                    UserBannedChannelId = 0,
                    UserUnbannedChannelId = 0,
                    UserJoinedChannelId = 0,
                    UserLeftChannelId = 0,
                },
                Leveling = new Leveling
                {
                    Enabled = false,
                    AnnouncementChannelId = 0,
                    PointsToLevelUp = 0,
                    AfkChannelId = 0,
                    LevelRoles = Array.Empty<LevelRole>().ToList()
                },
                MovieEvents = new MovieEvents
                {
                    Enabled = false,
                    AnnouncementChannelId = 0,
                    RoleId = 0,
                    StreamingChannelId = 0
                },
                TemporaryChannels = new TemporaryChannels
                {
                    Enabled = false,
                    CategoryId = 0,
                    CreateChannelId = 0
                },
                TourEvents = new TourEvents
                {
                    Enabled = false,
                    AnnouncementChannelId = 0,
                    RoleId = 0
                },
                Suggestions = new Suggestions
                {
                    Enabled = false,
                    AnnouncementChannelId = 0
                }
        }
        };
        var users = _client.GetGuild(guildId).Users.Where(x => !x.IsBot);
        var usersToAdd = users.Select(human => new User
            {
                Points = 0,
                Level = 0,
                UserId = human.Id,
                LastDailyClaim = DateTime.MinValue,
                LastVoiceChannelJoin = DateTime.MinValue,
                Warns = Array.Empty<Warn>().ToList()
            })
            .ToList();
        guild.Users = usersToAdd;
        await _collection.InsertOneAsync(guild).ConfigureAwait(false);
        return guild;
    }

    private async ValueTask<GuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        return guild.Config;
    }

    public async ValueTask<GuildConfig> GetGuildConfigFromCacheAsync(ulong guildId)
    {
        var config = await _cache.GetOrCreateAsync(guildId.ToString(), async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await GetGuildConfigAsync(guildId).ConfigureAwait(false);
        }).ConfigureAwait(false);
        return config;
    }

    private async ValueTask<User> RegisterUserAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Users.Add(new User
        {
            Points = 0,
            Level = 0,
            UserId = userId,
            LastDailyClaim = DateTime.MinValue,
            Warns = Array.Empty<Warn>().ToList()
        });
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return guild.Users.Find(x => x.UserId == userId);
    }
    public async ValueTask<List<Warn>> GetWarnsAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        var warns = user?.Warns;
        return warns;
    }

    public async Task AddWarnAsync(ulong guildId, ulong userId, ulong moderatorId, string reason)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Warns.Add(new Warn(moderatorId, reason, DateTime.UtcNow));
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task<bool> RemoveWarnAsync(ulong guildId, ulong userId, int warnId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        if (user.Warns.Count < warnId)
        {
            return false;
        }
        user.Warns.RemoveAt(warnId - 1);
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return true;
    }

    public async Task<int> AddPointsAsync(ulong guildId, ulong userId, int pointsToAdd)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Points += pointsToAdd;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Points;
    }
    public async Task<int> SetPointsAsync(ulong guildId, ulong userId, int points)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Points = points;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Points;
    }

    public async Task<int> GetPointsAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user?.Points ?? 0;
    }

    public async Task<int> AddLevelAsync(ulong guildId, ulong userId, int levelsToAdd)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Level += levelsToAdd;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Level;
    }
    public async Task<int> SetLevelAsync(ulong guildId, ulong userId, int level)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Level = level;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Level;
    }

    public async Task<int> GetLevelAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user?.Level ?? 0;
    }

    public async Task<List<User>> GetTopAsync(ulong guildId, int users)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var pointsPerLevel = (await GetGuildConfigAsync(guildId).ConfigureAwait(false)).Leveling.PointsToLevelUp;
        guild.Users.ForEach(x => x.Points += x.Level * pointsPerLevel);
        return guild.Users.OrderByDescending(x => x.Points).Take(users).ToList();
    }

    public async Task<DateTime> GetDailyClaimDateAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user.LastDailyClaim;
    }

    public async Task SetDailyClaimDateAsync(ulong guildId, ulong userId, DateTime now)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.LastDailyClaim = now;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task SetVoiceChannelJoinDateAsync(ulong guildId, ulong userId, DateTime now)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.LastVoiceChannelJoin = now;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task<DateTime> GetVoiceChannelJoinDateAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user.LastVoiceChannelJoin;
    }

    public async Task SetOsuIdAsync(ulong guildId, ulong userId, ulong osuId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.OsuId = osuId;
        guild.Users.Remove(user);
        guild.Users.Add(user);

        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }
    public async Task<ulong> GetOsuIdAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user.OsuId;
    }

    public async Task<List<(ulong userId, ulong osuId)>> GetOsuIdsAsync(ulong guildId, int limit)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);

        return guild.Users.Where(x => x.OsuId != 0).Select(x => (x.UserId, x.OsuId)).Take(limit).ToList();
    }

    public async Task SaveGuildConfigAsync(ulong guildId, GuildConfig config)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Config = config;
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        _cache.Remove(guildId.ToString());
    }
}