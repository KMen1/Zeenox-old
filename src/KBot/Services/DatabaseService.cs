using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Models;
using KBot.Models.Guild;
using KBot.Models.User;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

namespace KBot.Services;

public class DatabaseService : IInjectable
{
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly IMongoCollection<Guild> _collection;

    public DatabaseService(BotConfig config, IMemoryCache cache, IMongoDatabase database, DiscordSocketClient client)
    {
        _cache = cache;
        _client = client;
        _collection = database.GetCollection<Guild>(config.MongoDb.Collection);
    }

    public async Task FixUsersAsync(SocketGuild vguild)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vguild.Id).ConfigureAwait(false)).First();
        foreach (var user in vguild.Users)
        {
            if (!guild.Users.Exists(x => x.Id == user.Id))
            {
                guild.Users.Add(new User(user));
            }
        }
        foreach (var guildUser in guild.Users)
        {
            guildUser.Roles ??= new List<ulong>();
            guildUser.Gambling ??= new Gambling();
            guildUser.Transactions ??= new List<Transaction>();
            guildUser.Warns ??= new List<Warn>();
            if (guildUser.Level == 0)
                guildUser.Level = 1;
        }
        await _collection.ReplaceOneAsync(x => x.DocId == guild.DocId, guild).ConfigureAwait(false);
    }

    public Task AddGuildAsync(List<SocketGuildUser> users)
    {
        var guild = new Guild(users);
        return _collection.InsertOneAsync(guild);
    }

    public Task<GuildConfig> GetGuildConfigAsync(IGuild guild)
    {
        return _cache.GetOrCreateAsync(guild.Id.ToString(), async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return (await _collection.FindAsync(y => y.Id == guild.Id).ConfigureAwait(false)).First().Config;
        });
    }

    public async Task<Guild> GetGuildAsync(IGuild vGuild)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        return guild;
    }

    public async Task AddReactionRoleMessageAsync(IGuild vGuild, ButtonRoleMessage message)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        guild.ButtonRoles.Add(message);
        var filter = Builders<Guild>.Filter.Eq(x => x.Id, vGuild.Id);
        var update = Builders<Guild>.Update.Set(x => x.ButtonRoles, guild.ButtonRoles);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
    }

    public async Task<ButtonRoleMessage> GetReactionRoleMessagesAsync(IGuild vGuild, ulong messageId)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        return guild.ButtonRoles.Find(x => x.MessageId == messageId);
    }

    public async Task<(bool, ButtonRoleMessage)> UpdateReactionRoleMessageAsync(IGuild vGuild, ulong messageId,
        Action<ButtonRoleMessage> action)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        var index = guild.ButtonRoles.FindIndex(x => x.MessageId == messageId);
        if (index == -1) return (false, null);
        action(guild.ButtonRoles[index]);
        var filter = Builders<Guild>.Filter.Eq(x => x.Id, vGuild.Id);
        var update = Builders<Guild>.Update.Set(x => x.ButtonRoles, guild.ButtonRoles);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
        return (true, guild.ButtonRoles[index]);
    }

    public async Task AddUserAsync(IGuild vGuild, SocketGuildUser user)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        guild.Users.Add(new User(user));
        var filter = Builders<Guild>.Filter.Eq(x => x.Id, vGuild.Id);
        var update = Builders<Guild>.Update.Set(x => x.Users, guild.Users);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
    }

    public async Task<bool> CheckIfGuildIsInDbAsync(IGuild guild)
    {
        return await (await _collection.FindAsync(x => x.Id == guild.Id).ConfigureAwait(false))
            .AnyAsync().ConfigureAwait(false);
    }

    public async ValueTask<User> GetUserAsync(IGuild vGuild, SocketUser vUser)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        return guild.Users.Find(x => x.Id == vUser.Id);
    }

    public async Task<User> UpdateUserAsync(IGuild vGuild, SocketUser user, Action<User> action)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        var index = guild.Users.FindIndex(x => x.Id == user.Id);
        if (index == -1)
        {
            guild.Users.Add(new User(user,
                (await vGuild.GetUserAsync(user.Id).ConfigureAwait(false)).RoleIds.ToList()));
            index = guild.Users.Count - 1;
        }

        var dbUser = guild.Users[index];
        action(dbUser);
        var filter = Builders<Guild>.Filter.Eq(x => x.Id, vGuild.Id);
        var update = Builders<Guild>.Update.Set(x => x.Users[index], dbUser);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
        return dbUser;
    }

    public async Task<User> UpdateBotUserAsync(IGuild vGuild, Action<User> action)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        var index = guild.Users.FindIndex(x => x.Id == _client.CurrentUser.Id);
        var dbUser = guild.Users[index];
        action(dbUser);
        var filter = Builders<Guild>.Filter.Eq(x => x.Id, vGuild.Id);
        var update = Builders<Guild>.Update.Set(x => x.Users[index], dbUser);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
        return dbUser;
    }

    public async Task<List<User>> GetTopUsersAsync(IGuild vGuild, int users)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        guild.Users.ForEach(x => x.Xp += x.TotalXp);
        return guild.Users.OrderByDescending(x => x.Xp).Take(users).ToList();
    }

    public async Task<List<(ulong userId, ulong osuId)>> GetOsuIdsAsync(ulong guildId, int limit)
    {
        var guild = (await _collection.FindAsync(x => x.Id == guildId).ConfigureAwait(false)).First();
        return guild.Users.Where(x => x.OsuId != 0).Select(x => (x.Id, x.OsuId)).Take(limit).ToList();
    }

    public async Task UpdateGuildConfigAsync(IGuild vGuild, GuildConfig config)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        guild.Config = config;
        await _collection.ReplaceOneAsync(x => x.DocId == guild.DocId, guild).ConfigureAwait(false);
        _cache.Remove(vGuild.Id.ToString());
    }

    public async Task<GuildConfig> UpdateGuildConfigAsync(IGuild guild, Action<GuildConfig> action)
    {
        var config = (await _collection.FindAsync(x => x.Id == guild.Id).ConfigureAwait(false)).First().Config;
        action(config);
        var filter = Builders<Guild>.Filter.Eq(x => x.Id, guild.Id);
        var update = Builders<Guild>.Update.Set(x => x.Config, config);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
        _cache.Remove(guild.Id.ToString());
        return config;
    }

    public async Task UpdateGuildAsync(IGuild vGuild, Action<Guild> action)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        action(guild);
        await _collection.ReplaceOneAsync(x => x.Id == vGuild.Id, guild).ConfigureAwait(false);
    }

    public async Task<(bool, bool)> GetGambleValuesAsync(IGuild vGuild, IUser user, int bet)
    {
        var guild = (await _collection.FindAsync(x => x.Id == vGuild.Id).ConfigureAwait(false)).First();
        var userModel = guild.Users.Find(x => x.Id == user.Id);
        var botModel = guild.Users.Find(x => x.Id == _client.CurrentUser.Id);
        return (userModel!.Money >= bet, bet < botModel!.Money);
    }
}