using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using KBot.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KBot.Services;

public class DatabaseService
{
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildModel> _collection;

    public DatabaseService(BotConfig config, IMemoryCache cache, IMongoDatabase database)
    {
        _cache = cache;
        _collection = database.GetCollection<GuildModel>(config.MongoDb.Collection);
    }

    public async Task Update(SocketGuild vguild)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vguild.Id).ConfigureAwait(false)).First();
        foreach (var guildUser in guild.Users)
        {
            guildUser.Gambling = new GamblingProfile();
        }
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }

    public Task AddGuildAsync(List<SocketGuildUser> users)
    {
        var guild = new GuildModel(users);
        return _collection.InsertOneAsync(guild);
    }

    public Task<GuildConfig> GetGuildConfigAsync(IGuild guild)
    {
        return _cache.GetOrCreateAsync(guild.Id.ToString(), async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return (await _collection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false)).First().Config;
        });
    }
    public async Task AddUserAsync(IGuild vGuild, SocketUser user)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).First();
        guild.Users.Add(new User(user.Id));
        var filter = Builders<GuildModel>.Filter.Eq(x => x.GuildId, vGuild.Id);
        var update = Builders<GuildModel>.Update.Set(x => x.Users, guild.Users);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
    }

    public async Task<bool> CheckIfGuildIsInDbAsync(IGuild guild)
    {
        return await (await _collection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false))
            .AnyAsync().ConfigureAwait(false);
    }
    
    public async ValueTask<User> GetUserAsync(IGuild vGuild, SocketUser vUser)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).First();
        return guild.Users.Find(x => x.Id == vUser.Id);
    }
    public async Task<User> UpdateUserAsync(IGuild vGuild, SocketUser user, Action<User> action)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).First();
        var index = guild.Users.FindIndex(x => x.Id == user.Id);
        var dbUser = guild.Users[index];
        action(dbUser);
        var filter = Builders<GuildModel>.Filter.Eq(x => x.GuildId, vGuild.Id);
        var update = Builders<GuildModel>.Update.Set(x => x.Users[index], dbUser);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
        return dbUser;
    }
    public async Task<List<User>> GetTopAsync(IGuild vGuild, int users)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).First();
        guild.Users.ForEach(x => x.XP += GetTotalXP(x.Level));
        return guild.Users.OrderByDescending(x => x.XP).Take(users).ToList();
    }
    private static int GetTotalXP(int level)
    {
        var total = 0;
        for (var i = 0; i < level; i++)
        {
            total += (int)Math.Pow(i * 4, 2);
        }
        return total;
    }

    public async Task<List<(ulong userId, ulong osuId)>> GetOsuIdsAsync(ulong guildId, int limit)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).First();
        return guild.Users.Where(x => x.OsuId != 0).Select(x => (x.Id, x.OsuId)).Take(limit).ToList();
    }

    public async Task UpdateGuildConfigAsync(IGuild vGuild, GuildConfig config)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).First();
        guild.Config = config;
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        _cache.Remove(vGuild.Id.ToString());
    }
}