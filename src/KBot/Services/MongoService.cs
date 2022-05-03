using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Models;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

namespace KBot.Services;

public class MongoService : IInjectable
{
    private readonly IMongoCollection<SelfRoleMessage> _brCollection;
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildConfig> _configCollection;
    private readonly IMongoCollection<Transaction> _transactionCollection;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IMongoCollection<Warn> _warnCollection;

    public MongoService(BotConfig config, IMemoryCache cache, IMongoDatabase database)
    {
        _cache = cache;
        _configCollection = database.GetCollection<GuildConfig>(config.MongoDb.ConfigCollection);
        _userCollection = database.GetCollection<User>(config.MongoDb.UserCollection);
        _transactionCollection = database.GetCollection<Transaction>(config.MongoDb.TransactionCollection);
        _warnCollection = database.GetCollection<Warn>(config.MongoDb.WarnCollection);
        _brCollection = database.GetCollection<SelfRoleMessage>(config.MongoDb.ButtonRoleCollection);
    }

    public Task CreateGuildConfigAsync(IGuild guild)
    {
        return _configCollection.InsertOneAsync(new GuildConfig(guild));
    }

    public Task<GuildConfig> GetGuildConfigAsync(IGuild guild)
    {
        return _cache.GetOrCreateAsync(guild.Id.ToString(CultureInfo.InvariantCulture), async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return (await _configCollection.FindAsync(y => y.GuildId == guild.Id).ConfigureAwait(false))
                .FirstOrDefault();
        });
    }

    public async Task<bool> IsGuildRegisteredAsync(IGuild guild)
    {
        return await (await _configCollection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false)).AnyAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<ulong>> GetDbdNotificationChannelIds()
    {
        return (await _configCollection.FindAsync(x => x.DbdNotificationChannelId != 0).ConfigureAwait(false)).ToList()
            .Select(x => x.DbdNotificationChannelId);
    }

    public async Task<IEnumerable<ulong>> GetEpicNotificationChannelIds()
    {
        return (await _configCollection.FindAsync(x => x.EpicNotificationChannelId != 0).ConfigureAwait(false)).ToList()
            .Select(x => x.EpicNotificationChannelId);
    }

    public Task AddSelfRoleMessageAsync(SelfRoleMessage message)
    {
        return _brCollection.InsertOneAsync(message);
    }

    public async Task<SelfRoleMessage> GetSelfRoleMessageAsync(ulong messageId)
    {
        var message = await _brCollection.FindAsync(x => x.MessageId == messageId).ConfigureAwait(false);
        return message.FirstOrDefault();
    }

    public async Task<(bool, SelfRoleMessage?)> UpdateReactionRoleMessageAsync(IGuild vGuild, ulong messageId,
        Action<SelfRoleMessage> action)
    {
        var messages = await _brCollection.FindAsync(x => x.GuildId == vGuild.Id && x.MessageId == messageId)
            .ConfigureAwait(false);
        var message = messages.FirstOrDefault();
        if (message is null)
            return (false, null);
        action(message);
        await _brCollection.ReplaceOneAsync(x => x.GuildId == vGuild.Id && x.MessageId == messageId, message)
            .ConfigureAwait(false);
        return (true, message);
    }

    private async Task<User> AddUserAsync(SocketGuildUser user)
    {
        var users = await _userCollection.FindAsync(x => x.UniqueId == user.Id + user.Guild.Id)
            .ConfigureAwait(false);
        if (await users.AnyAsync().ConfigureAwait(false))
            return users.First();
        var newUser = new User(user);
        await _userCollection.InsertOneAsync(newUser).ConfigureAwait(false);
        return newUser;
    }

    public async ValueTask<User> GetUserAsync(SocketGuildUser vUser)
    {
        return (await _userCollection.FindAsync(x => x.UniqueId == vUser.Guild.Id + vUser.Id)
            .ConfigureAwait(false)).FirstOrDefault() ?? await AddUserAsync(vUser).ConfigureAwait(false);
    }

    public async Task<User> UpdateUserAsync(SocketGuildUser user, Action<User> action)
    {
        var dbUser = await GetUserAsync(user).ConfigureAwait(false);
        action(dbUser);
        await _userCollection.ReplaceOneAsync(x => x.UniqueId == dbUser.UniqueId, dbUser)
            .ConfigureAwait(false);
        return dbUser;
    }

    public async Task AddWarnAsync(Warn warn, SocketGuildUser user)
    {
        await _warnCollection.InsertOneAsync(warn).ConfigureAwait(false);
        await UpdateUserAsync(user, x => x.WarnIds.Add(warn.Id)).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Warn>> GetWarnsAsync(SocketGuildUser user)
    {
        var dbUser = await GetUserAsync(user).ConfigureAwait(false);

        var warns = new List<Warn>();
        var tasks = dbUser.WarnIds.ConvertAll(warnId =>
            _warnCollection.FindAsync(x => x.Id == warnId)
                .ContinueWith(async x =>
                    warns.Add((await x.ConfigureAwait(false)).FirstOrDefault())));
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return warns;
    }

    public async Task<Warn?> GetWarnAsync(string warnId)
    {
        var warns = await _warnCollection.FindAsync(x => x.Id == warnId).ConfigureAwait(false);
        return warns.FirstOrDefault();
    }

    public async Task<bool> RemoveWarnAsync(string warnId)
    {
        var warns = await _warnCollection.FindAsync(x => x.Id == warnId).ConfigureAwait(false);
        var warn = warns.FirstOrDefault();
        if (warn is null)
            return false;
        var dbUser =
            (await _userCollection.FindAsync(x => x.GuildId == warn.GuildId && x.UserId == warn.GivenToId)
                .ConfigureAwait(false)).FirstOrDefault();
        dbUser.WarnIds.Remove(warnId);
        await _userCollection.ReplaceOneAsync(x => x.GuildId == warn.GuildId && x.UserId == warn.GivenToId, dbUser)
            .ConfigureAwait(false);
        await _warnCollection.DeleteOneAsync(x => x.Id == warnId).ConfigureAwait(false);
        return true;
    }

    public async Task<IEnumerable<User>> GetTopUsersAsync(IGuild vGuild, int limit)
    {
        var users = (await _userCollection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).ToList();
        return users.OrderByDescending(x => x.Level).Take(limit).ToList();
    }

    public async Task UpdateGuildConfigAsync(IGuild guild, Action<GuildConfig> action)
    {
        var config = (await _configCollection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false))
            .FirstOrDefault();
        action(config);
        await _configCollection.ReplaceOneAsync(x => x.GuildId == config.GuildId, config).ConfigureAwait(false);
        _cache.Remove(guild.Id.ToString(CultureInfo.InvariantCulture));
    }

    public async Task AddTransactionAsync(Transaction transaction, SocketGuildUser? user)
    {
        await _transactionCollection.InsertOneAsync(transaction).ConfigureAwait(false);
        if (user is null)
            return;
        await UpdateUserAsync(user, x => x.TransactionIds.Add(transaction.Id)).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(SocketGuildUser user)
    {
        var dbUser = await GetUserAsync(user).ConfigureAwait(false);

        var transactions = new List<Transaction>();
        var tasks = dbUser.TransactionIds.ConvertAll(transactionId =>
            _transactionCollection.FindAsync(x => x.Id == transactionId)
                .ContinueWith(async x =>
                    transactions.Add((await x.ConfigureAwait(false)).FirstOrDefault())));
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return transactions;
    }

    public async Task<Transaction?> GetTransactionAsync(string id)
    {
        return (await _transactionCollection.FindAsync(x => x.Id == id).ConfigureAwait(false)).FirstOrDefault();
    }
}