using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Zeenox.Extensions;
using Zeenox.Models;

namespace Zeenox.Services;

public class MongoService
{
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildConfig> _configs;
    private readonly IMongoCollection<User> _users;

    public MongoService(IMongoClient mongoClient, IMemoryCache cache)
    {
        _cache = cache;
        var database = mongoClient.GetDatabase(
            Environment.GetEnvironmentVariable("MONGO_DATABASE")
        );
        _configs = database.GetCollection<GuildConfig>("configs");
        _users = database.GetCollection<User>("users");

        Task.Run(CacheConfigsAsync);
        RecurringJob.AddOrUpdate("cache_guild_configs", () => CacheConfigsAsync(), "*/10 * * * *");
    }

    public async Task CacheConfigsAsync()
    {
        var cursor = await _configs
            .FindAsync(FilterDefinition<GuildConfig>.Empty)
            .ConfigureAwait(false);
        var configs = await cursor.ToListAsync().ConfigureAwait(false);

        foreach (var config in configs) _cache.SetGuildConfig(config);
    }

    public async Task<GuildConfig> AddGuildConfigAsync(ulong guildId)
    {
        var cursor = await _configs.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        if (await cursor.AnyAsync().ConfigureAwait(false))
            return _cache.GetGuildConfig(guildId);
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
        _cache.SetGuildConfig(config);
        return config;
    }

    public async Task<User> GetUserAsync(ulong id)
    {
        var cursor = await _users.FindAsync(x => x.Id == id).ConfigureAwait(false);
        return await cursor.SingleOrDefaultAsync().ConfigureAwait(false)
               ?? await AddUserAsync(id).ConfigureAwait(false);
    }

    private async Task<User> AddUserAsync(ulong id)
    {
        var user = new User(id);
        await _users.InsertOneAsync(user).ConfigureAwait(false);
        return user;
    }

    public async Task<User> UpdateUserAsync(ulong id, Action<User> action)
    {
        var user = await GetUserAsync(id).ConfigureAwait(false);
        action(user);
        await _users.ReplaceOneAsync(x => x.Id == id, user).ConfigureAwait(false);
        return user;
    }
}