using System;
using System.Collections.Generic;
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
    private readonly IMemoryCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly IMongoCollection<GuildConfig> _configCollection;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IMongoCollection<Transaction> _transactionCollection;
    private readonly IMongoCollection<Warn> _warnCollection;
    private readonly IMongoCollection<ButtonRoleMessage> _brCollection;

    public MongoService(BotConfig config, IMemoryCache cache, IMongoDatabase database, DiscordSocketClient client)
    {
        _cache = cache;
        _client = client;
        _configCollection = database.GetCollection<GuildConfig>(config.MongoDb.ConfigCollection);
        _userCollection = database.GetCollection<User>(config.MongoDb.UserCollection);
        _transactionCollection = database.GetCollection<Transaction>(config.MongoDb.TransactionCollection);
        _warnCollection = database.GetCollection<Warn>(config.MongoDb.WarnCollection);
        _brCollection = database.GetCollection<ButtonRoleMessage>(config.MongoDb.ButtonRoleCollection);
    }

    public Task CreateGuildConfigAsync(IGuild guild)
    {
        return _configCollection.InsertOneAsync(new GuildConfig(guild));
    }
    
    public Task<GuildConfig> GetGuildConfigAsync(IGuild guild)
    {
        return _cache.GetOrCreateAsync(guild.Id.ToString(), async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return (await _configCollection.FindAsync(y => y.GuildId == guild.Id).ConfigureAwait(false)).FirstOrDefault();
        });
    }

    public Task AddReactionRoleMessageAsync(ButtonRoleMessage message)
    {
        return _brCollection.InsertOneAsync(message);
    }

    public async Task<ButtonRoleMessage> GetReactionRoleMessagesAsync(IGuild vGuild, ulong messageId)
    {
        var message = await _brCollection.FindAsync(x => x.GuildId == vGuild.Id && x.MessageId == messageId).ConfigureAwait(false);
        return message.FirstOrDefault();
    }

    public async Task<(bool, ButtonRoleMessage)> UpdateReactionRoleMessageAsync(IGuild vGuild, ulong messageId,
        Action<ButtonRoleMessage> action)
    {
        var messages = await _brCollection.FindAsync(x => x.GuildId == vGuild.Id && x.MessageId == messageId).ConfigureAwait(false);
        var message = messages.FirstOrDefault();
        if (message is null)
            return (false, null);
        action(message);
        await _brCollection.ReplaceOneAsync(x => x.GuildId == vGuild.Id && x.MessageId == messageId, message).ConfigureAwait(false);
        return (true, message);
    }

    public async Task<User> AddUserAsync(SocketGuildUser user)
    {
        var users = await _userCollection.FindAsync(x => x.UserId == user.Id && x.GuildId == user.Guild.Id).ConfigureAwait(false);
        if (await users.AnyAsync().ConfigureAwait(false))
            return users.First();
        var newUser = new User(user);
        await _userCollection.InsertOneAsync(newUser).ConfigureAwait(false);
        return newUser;
    }

    public async ValueTask<User> GetUserAsync(SocketGuildUser vUser)
    {
        var user = (await _userCollection.FindAsync(x => x.GuildId == vUser.Guild.Id && x.UserId == vUser.Id)
            .ConfigureAwait(false)).FirstOrDefault();
        if (user is null)
            return await AddUserAsync(vUser).ConfigureAwait(false);
        return user;
    }

    public async Task<User> UpdateUserAsync(IGuild vGuild, SocketUser user, Action<User> action)
    {
        var dbUser = (await _userCollection.FindAsync(x => x.GuildId == vGuild.Id && x.UserId == user.Id).ConfigureAwait(false)).FirstOrDefault();
        if (dbUser is null)
            await AddUserAsync(user as SocketGuildUser).ConfigureAwait(false);
        action(dbUser);
        await _userCollection.ReplaceOneAsync(x => x.GuildId == vGuild.Id && x.UserId == user.Id, dbUser).ConfigureAwait(false);
        return dbUser;
    }

    public Task AddWarnAsync(Warn warn)
    {
        return _warnCollection.InsertOneAsync(warn);
    }
    
    public async Task<List<Warn>> GetWarnsAsync(SocketGuildUser user)
    {
        var warns = await _warnCollection.FindAsync(x => x.GuildId == user.Guild.Id && x.GivenToId == user.Id).ConfigureAwait(false);
        return warns.ToList();
    }
    
    public async Task<Warn> GetWarnAsync(string warnId)
    {
        var warns = await _warnCollection.FindAsync(x => x.WarnId == warnId).ConfigureAwait(false);
        return warns.FirstOrDefault();
    }

    public async Task<bool> RemoveWarnAsync(string warnId)
    {
        var result = await _warnCollection.DeleteOneAsync(x => x.WarnId == warnId).ConfigureAwait(false);
        return result.IsAcknowledged;
    }

    public async Task<List<User>> GetTopUsersAsync(IGuild vGuild, int limit)
    {
        var users = (await _userCollection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).ToList();
        return users.OrderByDescending(x => x.Xp).Take(limit).ToList();
    }

    public async Task<List<(ulong userId, ulong osuId)>> GetOsuIdsAsync(IGuild guild, int limit)
    {
        var users = await _userCollection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false);
        return users.ToList().Where(x => x.OsuId != 0).Take(limit).Select(x => (x.UserId, x.OsuId)).ToList();
    }

    public async Task UpdateGuildConfigAsync(IGuild vGuild, GuildConfig config)
    {
        await _configCollection.ReplaceOneAsync(x => x.GuildId == config.GuildId, config).ConfigureAwait(false);
        _cache.Remove(vGuild.Id.ToString());
    }

    public async Task UpdateGuildConfigAsync(IGuild guild, Action<GuildConfig> action)
    {
        var config = (await _configCollection.FindAsync(x => x.GuildId == guild.Id).ConfigureAwait(false)).FirstOrDefault();
        action(config);
        await _configCollection.ReplaceOneAsync(x => x.GuildId == config.GuildId, config).ConfigureAwait(false);
        _cache.Remove(guild.Id.ToString());
    }

    public Task AddTransactionAsync(Transaction transaction)
    {
        return _transactionCollection.InsertOneAsync(transaction);
    }
    
    public async Task<List<Transaction>> GetTransactionsAsync(IGuild guild, SocketUser user)
    {
        var users = await _userCollection.FindAsync(x => x.GuildId == guild.Id && x.UserId == user.Id).ConfigureAwait(false);
        var dbUser = users.FirstOrDefault();

        var transactions = new List<Transaction>();
        foreach (var transactionId in dbUser.TransactionIds)
        {
            transactions.Add((await _transactionCollection.FindAsync(x => x.Id == transactionId).ConfigureAwait(false)).FirstOrDefault());
        }

        return transactions;
    }
    
    public async Task<Transaction> GetTransactionAsync(string id)
    {
        return (await _transactionCollection.FindAsync(x => x.Id == id).ConfigureAwait(false)).FirstOrDefault();
    }
    
}