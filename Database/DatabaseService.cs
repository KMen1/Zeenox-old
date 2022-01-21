using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using KBot.Config;
using MongoDB.Driver;

namespace KBot.Database;

public class DatabaseService
{
    private readonly ConfigModel.Config _config;
    private readonly DiscordSocketClient _client;
    
    public DatabaseService(ConfigModel.Config config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
        client.Ready += RegisterGuilds;
        client.JoinedGuild += RegisterNewGuild;
    }

    private async Task RegisterNewGuild(SocketGuild arg)
    {
        await RegisterGuild(arg.Id);
    }

    private async Task RegisterGuilds()
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);
        foreach (var guild in _client.Guilds)
        {
            var isGuildInDb = await collection.FindAsync(x => x.GuildId == guild.Id).Result.AnyAsync();
            if (!isGuildInDb)
            {
                await RegisterGuild(guild.Id);
            }
        }
    }

    public async Task<GuildModel> RegisterGuild(ulong guildId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);
        
        var guild = new GuildModel
        {
            GuildId = guildId
        };
        var guildUsers = _client.GetGuild(guildId).Users;
        var humans = guildUsers.Where(x => !x.IsBot);
        var usersToAdd = humans.Select(human => new UserModel
            {
                Points = 0,
                Level = 0,
                UserId = human.Id,
                LastDailyClaim = DateTime.MinValue,
                Warns = Array.Empty<WarnObject>().ToList()
            })
            .ToList();
        guild.Users = usersToAdd;
        await collection.InsertOneAsync(guild);
        return guild;
    }
    
    private async Task<UserModel> RegisterUser(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);
        
        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        guild.Users.Add(new UserModel
        {
            Points = 0,
            Level = 0,
            UserId = userId,
            LastDailyClaim = DateTime.MinValue,
            Warns = Array.Empty<WarnObject>().ToList()
        });
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
        return guild.Users.FirstOrDefault(x => x.UserId == userId);
    }
    public async Task<List<WarnObject>> GetWarnsByUserId(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);
        
        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        var warns = user?.Warns;
        return warns;
    }

    public async Task AddWarnByUserId(ulong guildId, ulong userId, ulong moderatorId, string reason)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);
 
        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        user.Warns.Add(new WarnObject {ModeratorId = moderatorId, Reason = reason, Date = DateTime.UtcNow});
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
    }
    
    public async Task<bool> RemoveWarnByUserId(ulong guildId, ulong userId, int warnId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        if (user.Warns.Count < warnId)
        {
            return false;
        }
        user.Warns.RemoveAt(warnId - 1);
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
        return true;
    }
    
    public async Task<int> AddPointsByUserId(ulong guildId, ulong userId, int pointsToAdd)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        user.Points += pointsToAdd;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
        return user.Points;
    }
    public async Task<int> SetPointsByUserId(ulong guildId, ulong userId, int points)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        user.Points = points;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
        return user.Points;
    }

    public async Task<int> GetUserPointsById(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        return user?.Points ?? 0;
    }

    public async Task<int> AddLevelByUserId(ulong guildId, ulong userId, int levelsToAdd)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        user.Level += levelsToAdd;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
        return user.Level;
    }
    public async Task<int> SetLevelByUserId(ulong guildId, ulong userId, int level)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        user.Level = level;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
        return user.Level;
    }
    
    public async Task<int> GetUserLevelById(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        return user?.Level ?? 0;
    }

    public async Task<DateTime> GetDailyClaimDateById(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        return user.LastDailyClaim;
    }

    public async Task SetDailyClaimDateById(ulong guildId, ulong userId, DateTime now)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        user.LastDailyClaim = now;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
    }
    
    public async Task SetUserVoiceChannelJoinDateById(ulong guildId, ulong userId, DateTime now)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        user.LastVoiceChannelJoin = now;
        guild.Users.Remove(user);
        guild.Users.Add(user);
        await collection.ReplaceOneAsync(x => x.Id == guild.Id, guild);
    }
    
    public async Task<DateTime> GetUserVoiceChannelJoinDateById(ulong guildId, ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<GuildModel>(_config.MongoDb.Collection);

        var guild = (await collection.FindAsync(x => x.GuildId == guildId)).ToList().FirstOrDefault() ?? await RegisterGuild(guildId);
        var user = guild.Users.FirstOrDefault(x => x.UserId == userId) ?? await RegisterUser(guildId, userId);
        return user.LastVoiceChannelJoin;
    }
    
}