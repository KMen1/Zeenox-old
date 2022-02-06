using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using KBot.Config;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KBot.Database;

public class DatabaseService : DiscordClientService
{
    private readonly BotConfig _config;
    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _cache;

    public DatabaseService(DiscordSocketClient client, ILogger<DatabaseService> logger, BotConfig config, IMemoryCache cache) : base(client, logger)
    {
        _config = config;
        _client = client;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.JoinedGuild += RegisterNewGuildAsync;
        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        
        await RegisterGuildsAsync().ConfigureAwait(false);
    }

    private Task RegisterNewGuildAsync(SocketGuild arg)
    {
        return RegisterGuildAsync(arg.Id);
    }

    private async Task RegisterGuildsAsync()
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);
        foreach (var guild in _client.Guilds)
        {
            var isGuildInDb = await (await collection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false))
                .AnyAsync().ConfigureAwait(false);
            if (!isGuildInDb)
            {
                await RegisterGuildAsync(guild.Id).ConfigureAwait(false);
            }
        }
    }

    private async Task<GuildModel> RegisterGuildAsync(ulong guildId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = new GuildModel
        {
            GuildId = guildId,
            Config = new GuildConfig
            {
                Announcements = new AnnouncementConfig
                {
                    Enabled = false,
                    UserBannedChannelId = 0,
                    UserUnbannedChannelId = 0,
                    UserJoinedChannelId = 0,
                    UserLeftChannelId = 0,
                },
                Leveling = new LevelingConfig
                {
                    Enabled = false,
                    AnnouncementChannelId = 0,
                    PointsToLevelUp = 0
                },
                MovieEvents = new MovieConfig
                {
                    Enabled = false,
                    AnnouncementChannelId = 0,
                    RoleId = 0,
                    StreamingChannelId = 0
                },
                TemporaryChannels = new TemporaryVoiceChannelConfig
                {
                    Enabled = false,
                    CategoryId = 0,
                    CreateChannelId = 0
                },
                TourEvents = new TourConfig
                {
                    Enabled = false,
                    AnnouncementChannelId = 0,
                    RoleId = 0
                },
                Suggestions = new SuggestionsConfig
                {
                    Enabled = false,
                    AnnouncementChannelId = 0
                }
        }
        };
        var guildUsers = _client.GetGuild(guildId).Users;
        var humans = guildUsers.Where(x => !x.IsBot);
        var usersToAdd = humans.Select(human => new User
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
        await collection.InsertOneAsync(guild).ConfigureAwait(false);
        return guild;
    }

    private async ValueTask<GuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        return guild.Config;
    }

    public async ValueTask<GuildConfig> GetGuildConfigFromCacheAsync(ulong guildId)
    {
        var config = await _cache.GetOrCreateAsync(guildId.ToString(), async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            return await GetGuildConfigAsync(guildId).ConfigureAwait(false);
        }).ConfigureAwait(false);
        return config;
    }

    private async ValueTask<User> RegisterUserAsync(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Users.Add(new User
        {
            Points = 0,
            Level = 0,
            UserId = userId,
            LastDailyClaim = DateTime.MinValue,
            Warns = Array.Empty<Warn>().ToList()
        });
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return guild.Users.Find(x => x.UserId == userId);
    }
    public async ValueTask<List<Warn>> GetWarnsByUserIdAsync(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        var warns = user?.Warns;
        return warns;
    }

    public async Task AddWarnByUserIdAsync(ulong guildId, ulong userId, ulong moderatorId, string reason)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Warns.Add(new Warn {ModeratorId = moderatorId, Reason = reason, Date = DateTime.UtcNow});
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task<bool> RemoveWarnByUserIdAsync(ulong guildId, ulong userId, int warnId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
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
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return true;
    }

    public async Task<int> AddPointsByUserIdAsync(ulong guildId, ulong userId, int pointsToAdd)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Points += pointsToAdd;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Points;
    }
    public async Task<int> SetPointsByUserIdAsync(ulong guildId, ulong userId, int points)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Points = points;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Points;
    }

    public async Task<int> GetUserPointsByIdAsync(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user?.Points ?? 0;
    }

    public async Task<int> AddLevelByUserIdAsync(ulong guildId, ulong userId, int levelsToAdd)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Level += levelsToAdd;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Level;
    }
    public async Task<int> SetLevelByUserIdAsync(ulong guildId, ulong userId, int level)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.Level = level;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return user.Level;
    }

    public async Task<int> GetUserLevelByIdAsync(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user?.Level ?? 0;
    }

    public async Task<List<User>> GetTopAsync(ulong guildId, int users)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var pointsPerLevel = (await GetGuildConfigAsync(guildId).ConfigureAwait(false)).Leveling.PointsToLevelUp;
        guild.Users.ForEach(x => x.Points += x.Level * pointsPerLevel);
        return guild.Users.OrderByDescending(x => x.Points).Take(users).ToList();
    }

    public async Task<DateTime> GetDailyClaimDateByIdAsync(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user.LastDailyClaim;
    }

    public async Task SetDailyClaimDateByIdAsync(ulong guildId, ulong userId, DateTime now)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.LastDailyClaim = now;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task SetUserVoiceChannelJoinDateByIdAsync(ulong guildId, ulong userId, DateTime now)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.LastVoiceChannelJoin = now;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task<DateTime> GetUserVoiceChannelJoinDateByIdAsync(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user.LastVoiceChannelJoin;
    }

    public async Task SetUserOsuIdAsync(ulong guildId, ulong userId, ulong osuId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        user.OsuId = osuId;
        guild.Users.Remove(user);
        guild.Users.Add(user);

        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }
    public async Task<ulong> GetUserOsuIdAsync(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == userId) ??
                   await RegisterUserAsync(guildId, userId).ConfigureAwait(false);
        return user.OsuId;
    }

    public async Task<List<(ulong userId, ulong osuId)>> GetOsuIdsAsync(ulong guildId, int limit)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);

        return guild.Users.Where(x => x.OsuId != 0).Select(x => (x.UserId, x.OsuId)).Take(limit).ToList();
    }

    public async Task SaveGuildConfigAsync(ulong guildId, GuildConfig config)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);
        
        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Config = config;
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        _cache.Set(guildId.ToString(), config);
    }
}