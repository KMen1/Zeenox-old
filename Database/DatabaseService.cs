using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using KBot.Config;
using MongoDB.Driver;
using Victoria;

namespace KBot.Database;

public class DatabaseService
{
    private readonly ConfigModel.Config _config;
    private readonly DiscordSocketClient _client;
    
    public DatabaseService(ConfigModel.Config config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
        client.Ready += RegisterGuildsAsync;
        client.JoinedGuild += RegisterNewGuildAsync;
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

    public async Task<GuildModel> RegisterGuildAsync(ulong guildId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = new GuildModel
        {
            GuildId = guildId,
            Audio = new Audio
            {
                IsLooping = false,
                EnabledFilter = string.Empty,
                NowPlayingMessageChannelId = 0,
                NowPlayingMessageId = 0,
                History = Array.Empty<AudioTrack>().ToList()
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

    public async Task SetNowPlayingMessageAsync(ulong guildId, ulong channelId, ulong messageId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Audio.NowPlayingMessageChannelId = channelId;
        guild.Audio.NowPlayingMessageId = messageId;

        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task<(ulong channelId, ulong messageId)> GetNowPlayingMessageAsync(ulong guildId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        return (guild.Audio.NowPlayingMessageChannelId, guild.Audio.NowPlayingMessageId);
    }

    public async Task SetEnabledFilterAsync(ulong guildId, string filter)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Audio.EnabledFilter = filter;

        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }
    public async Task<string> GetEnabledFilterAsync(ulong guildId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        return guild.Audio.EnabledFilter;
    }

    public async Task SetLoopEnabledAsync(ulong guildId, bool isLoop)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Audio.IsLooping = isLoop;

        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task<bool> GetLoopEnabledAsync(ulong guildId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        return guild.Audio.IsLooping;
    }

    public async Task AddTrackToHistoryAsync(ulong guildId, ulong userId, LavaTrack track)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Audio.History.Add(new AudioTrack
        {
            Track = track,
            UserId = userId
        });
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public async Task<AudioTrack> GetTrackFromHistoryAsync(ulong guildId, bool remove)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).ToList()
            .FirstOrDefault() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var history = guild.Audio.History;
        if (history.Count == 0)
        {
            return null;
        }
        var track = guild.Audio.History.Last();
        if (remove)
        {
            guild.Audio.History.Remove(track);
            await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        }
        return track;
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
}