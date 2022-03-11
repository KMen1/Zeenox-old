using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using KBot.Models;
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

    public async Task Update(SocketGuild vguild)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vguild.Id).ConfigureAwait(false)).First();
        foreach (var guildUser in guild.Users)
        {
            guildUser.Points = 0;
        }
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
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
        var users = _client.GetGuild(guildId).Users.Where(x => !x.IsBot).ToList();
        var guild = new GuildModel(guildId, users);
        await _collection.InsertOneAsync(guild).ConfigureAwait(false);
        return guild;
    }
    public async ValueTask<GuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        return await _cache.GetOrCreateAsync(guildId.ToString(), async x =>
        {
            x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return ((await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).First() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false)).Config;
        }).ConfigureAwait(false);
    }
    private async ValueTask<User> RegisterUserAsync(ulong guildId, ulong userId)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).First() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Users.Add(new User(userId));
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        return guild.Users.Find(x => x.UserId == userId);
    }
    public async ValueTask<User> GetUserAsync(SocketGuild vGuild, SocketUser vUser)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == vGuild.Id).ConfigureAwait(false)).First() ?? await RegisterGuildAsync(vGuild.Id).ConfigureAwait(false);
        var user = guild.Users.Find(x => x.UserId == vUser.Id) ??
                   await RegisterUserAsync(vGuild.Id, vUser.Id).ConfigureAwait(false);
        return user;
    }
    public async Task UpdateUserAsync(ulong guildId, User user)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).First() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        var index = guild.Users.FindIndex(x => x.UserId == user.UserId);
        guild.Users[index] = user;
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
    }
    public async Task<List<User>> GetTopAsync(ulong guildId, int users)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).First() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Users.ForEach(x => x.Points += GetTotalXP(x.Level));
        return guild.Users.OrderByDescending(x => x.Points).Take(users).ToList();
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
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).First() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);

        return guild.Users.Where(x => x.OsuId != 0).Select(x => (x.UserId, x.OsuId)).Take(limit).ToList();
    }

    public async Task SaveGuildConfigAsync(ulong guildId, GuildConfig config)
    {
        var guild = (await _collection.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false)).First() ?? await RegisterGuildAsync(guildId).ConfigureAwait(false);
        guild.Config = config;
        await _collection.ReplaceOneAsync(x => x.Id == guild.Id, guild).ConfigureAwait(false);
        _cache.Remove(guildId.ToString());
    }
}