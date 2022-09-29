using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discordance.Extensions;
using Discordance.Models;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

namespace Discordance.Services;

public class MongoService
{
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildConfig> _configs;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Warn> _warns;

    public MongoService(IMongoClient mongoClient, IMemoryCache cache)
    {
        _cache = cache;
        var database = mongoClient.GetDatabase(
            Environment.GetEnvironmentVariable("MONGO_DATABASE")
        );
        _configs = database.GetCollection<GuildConfig>("configs");
        _users = database.GetCollection<User>("users");
        _warns = database.GetCollection<Warn>("warns");

        Task.Run(CacheConfigsAsync);
        RecurringJob.AddOrUpdate("cache_guild_configs", () => CacheConfigsAsync(), "*/10 * * * *");
        RecurringJob.AddOrUpdate(
            "cache_notification_channels",
            () => CacheNotificationChannelsAsync(),
            "0 */12 * * *"
        );
    }

    public async Task CacheNotificationChannelsAsync()
    {
        var shrineCursor = await _configs
            .FindAsync(x => x.Notifications.WeeklyShrine)
            .ConfigureAwait(false);

        var epicCursor = await _configs
            .FindAsync(x => x.Notifications.WeeklyFreeGames)
            .ConfigureAwait(false);

        var shrine = shrineCursor
            .ToList()
            .Select(x => (x.GuildId, x.Notifications.ShrineChannelId));
        var epic = epicCursor.ToList().Select(x => (x.GuildId, x.Notifications.FreeGameChannelId));

        _cache.SetNotificationChannels(shrine, epic);
    }

    public async Task CacheConfigsAsync()
    {
        var cursor = await _configs
            .FindAsync(FilterDefinition<GuildConfig>.Empty)
            .ConfigureAwait(false);
        var configs = await cursor.ToListAsync().ConfigureAwait(false);

        foreach (var config in configs)
        {
            _cache.SetGuildConfig(config);
        }
    }

    public async Task<GuildConfig> AddGuildConfigAsync(ulong guildId)
    {
        var config = new GuildConfig(guildId);
        await _configs.InsertOneAsync(config).ConfigureAwait(false);
        _cache.SetGuildConfig(config);
        return config;
    }

    public async Task<GuildConfig> UpdateGuildConfig(ulong guildId, Action<GuildConfig> action)
    {
        var config = _cache.GetGuildConfig(guildId);
        action(config);
        await _configs.ReplaceOneAsync(x => x.GuildId == guildId, config).ConfigureAwait(false);
        return config;
    }

    /*
            #region Guilds
    
        public async Task<bool> AddWarnAsync(SocketGuildUser user, Warn warn)
        {
            var guild = await GetGuildAsync(user.Guild.Id).ConfigureAwait(false);
            if (guild is null)
                return false;
    
            if (!guild.Users.Exists(x => x.Id == user.Id))
                return false;
    
            var index = guild.Users.FindIndex(x => x.Id == user.Id);
            guild.Users[index].Warns.Add(warn);
            var update = Builders<Guild>.Update.Set(
                x => x.Users[index].Warns,
                guild.Users[index].Warns
            );
            await _guilds.UpdateOneAsync(x => x.Id == user.Guild.Id, update).ConfigureAwait(false);
            return true;
        }
    
        public async Task<bool> RemoveWarnAsync(SocketGuildUser user, string id)
        {
            var guild = await GetGuildAsync(user.Guild.Id).ConfigureAwait(false);
            if (guild is null)
                return false;
    
            if (!guild.Users.Exists(x => x.Id == user.Id))
                return false;
    
            var index = guild.Users.FindIndex(x => x.Id == user.Id);
            if (!guild.Users[index].Warns.Exists(x => x.Id == id))
                return false;
    
            var warnIndex = guild.Users[index].Warns.FindIndex(x => x.Id == id);
            guild.Users[index].Warns.RemoveAt(warnIndex);
    
            var update = Builders<Guild>.Update.Set(
                x => x.Users[index].Warns,
                guild.Users[index].Warns
            );
            await _guilds.UpdateOneAsync(x => x.Id == user.Guild.Id, update).ConfigureAwait(false);
            return true;
        }
    
        public async Task<bool> AddSelfRoleAsync(ulong guildId, SelfRoleMessage selfRoleMessage)
        {
            var guild = await GetGuildAsync(guildId).ConfigureAwait(false);
            if (guild is null)
                return false;
    
            guild.SelfRoles.Add(selfRoleMessage);
            var update = Builders<Guild>.Update.Set(x => x.SelfRoles, guild.SelfRoles);
            await _guilds.UpdateOneAsync(x => x.Id == guildId, update).ConfigureAwait(false);
            return true;
        }
    
        public async Task<bool> ReplaceSelfRoleAsync(ulong guildId, SelfRoleMessage newSelfRole)
        {
            var guild = await GetGuildAsync(guildId).ConfigureAwait(false);
            if (guild is null)
                return false;
    
            if (!guild.SelfRoles.Exists(x => x.MessageId == newSelfRole.MessageId))
                return false;
    
            var index = guild.SelfRoles.FindIndex(x => x.MessageId == newSelfRole.MessageId);
            var update = Builders<Guild>.Update.Set(x => x.SelfRoles[index], newSelfRole);
            await _guilds.UpdateOneAsync(x => x.Id == guildId, update).ConfigureAwait(false);
            return true;
        }
            #endregion
    */
    #region User

    public async Task<User> GetUserAsync(ulong id)
    {
        var cursor = await _users.FindAsync(x => x.Id == id).ConfigureAwait(false);
        return await cursor.SingleOrDefaultAsync().ConfigureAwait(false)
            ?? await AddUserAsync(id).ConfigureAwait(false);
    }

    public async Task<User> AddUserAsync(ulong id)
    {
        var user = new User(id);
        await _users.InsertOneAsync(user).ConfigureAwait(false);
        return user;
    }

    public async Task UpdateUserAsync(ulong id, Action<User> action)
    {
        var user = await GetUserAsync(id).ConfigureAwait(false);
        action(user);
        await _users.ReplaceOneAsync(x => x.Id == id, user).ConfigureAwait(false);
    }

    private async Task<GuildData> AddGuildData(SocketGuildUser user)
    {
        var data = new GuildData(user.Roles.Where(x => !x.IsEveryone).Select(y => y.Id));
        await UpdateUserAsync(user.Id, x => x.GuildDatas.Add(user.Guild.Id, data))
            .ConfigureAwait(false);
        return data;
    }

    public async Task<GuildData> GetGuildDataAsync(SocketGuildUser guildUser)
    {
        var user = await GetUserAsync(guildUser.Id).ConfigureAwait(false);

        if (!user.GuildDatas.ContainsKey(guildUser.Guild.Id))
            return await AddGuildData(guildUser).ConfigureAwait(false);

        return user.GuildDatas[guildUser.Guild.Id];
    }

    public async Task<GuildData> UpdateGuildDataAsync(
        SocketGuildUser guildUser,
        Action<GuildData> action
    )
    {
        var oldGuildData = await GetGuildDataAsync(guildUser).ConfigureAwait(false);
        action(oldGuildData);
        await UpdateUserAsync(guildUser.Id, x => x.GuildDatas[guildUser.Guild.Id] = oldGuildData)
            .ConfigureAwait(false);
        return oldGuildData;
    }

    #endregion

    #region Guild

    public async Task<List<User>> GetUsersInGuild(SocketGuild guild)
    {
        var userIds = guild.Users.Select(x => x.Id).ToList();
        var users = new List<User>();

        foreach (var userId in userIds)
        {
            users.Add(await GetUserAsync(userId).ConfigureAwait(false));
        }

        return users;
    }

    public async Task<IEnumerable<User>> GetUsersInGuild(ulong guildId)
    {
        return (
            await _users.FindAsync(x => x.GuildDatas.ContainsKey(guildId)).ConfigureAwait(false)
        ).ToEnumerable();
    }

    #endregion
}
