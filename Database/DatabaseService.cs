using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KBot.Config;
using MongoDB.Driver;

namespace KBot.Database;

public class DatabaseService
{
    private readonly ConfigModel.Config _config;
    
    public DatabaseService(ConfigModel.Config config)
    {
        _config = config;
    }

    private async Task RegisterUser(ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);
        await collection.InsertOneAsync(new UserModel()
        {
            Points = 0,
            UserId = userId,
            Warns = Array.Empty<WarnObject>().ToList()
        });
    }
    
    public async Task<List<WarnObject>> GetWarnsByUserId(ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);
        
        var user = await collection.FindAsync(x => x.UserId == userId);

        var warns = user.ToList().FirstOrDefault()?.Warns;
        return warns;
    }

    public async Task AddWarnByUserId(ulong userId, ulong moderatorId, string reason)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        if (user == null)
        {
            user = new UserModel
            {
                UserId = userId,
                Warns = new List<WarnObject> {new() { ModeratorId = moderatorId, Reason = reason, Date = DateTime.UtcNow}}
            };
            await collection.InsertOneAsync(user);
            return;
        }
        user.Warns.Add(new WarnObject {ModeratorId = moderatorId, Reason = reason, Date = DateTime.UtcNow});
        await collection.ReplaceOneAsync(x => x.Id == user.Id, user);
    }
    
    public async Task<bool> RemoveWarnByUserId(ulong userId, ulong moderatorId, int warnId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        if (user == null)
        {
            return false;
        }
        // check if warn index exists
        if (user.Warns.Count < warnId)
        {
            return false;
        }
        user.Warns.RemoveAt(warnId - 1);
        await collection.ReplaceOneAsync(x => x.Id == user.Id, user);
        return true;
    }
    
    public async Task<int> AddPointsByUserId(ulong userId, int pointsToAdd)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        if (user == null)
        {
            await RegisterUser(userId);
            user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        }
        user.Points += pointsToAdd;
        await collection.ReplaceOneAsync(x => x.Id == user.Id, user);
        return user.Points;
    }
    public async Task<int> SetPointsByUserId(ulong userId, int points)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        if (user == null)
        {
            await RegisterUser(userId);
            user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        }
        user.Points = points;
        await collection.ReplaceOneAsync(x => x.Id == user.Id, user);
        return user.Points;
    }

    public async Task<int> GetUserPointsById(ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        return user?.Points ?? 0;
    }

    public async Task<int> AddLevelByUserId(ulong userId, int levelsToAdd)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        if (user == null)
        {
            await RegisterUser(userId);
            user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        }
        user.Level += levelsToAdd;
        await collection.ReplaceOneAsync(x => x.Id == user.Id, user);
        return user.Level;
    }
    public async Task<int> SetLevelByUserId(ulong userId, int level)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        if (user == null)
        {
            await RegisterUser(userId);
            user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        }
        user.Level = level;
        await collection.ReplaceOneAsync(x => x.Id == user.Id, user);
        return user.Level;
    }
    
    public async Task<int> GetUserLevelById(ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        return user?.Level ?? 0;
    }
}