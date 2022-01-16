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

    public async Task AddWarnToUser(ulong userId, ulong moderatorId, string reason)
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
    
    /*public async Task RemoveWarnFromUser(ulong userId, ulong moderatorId)
    {
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        var collection = database.GetCollection<UserModel>(_warnCollectionName);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        if (user == null)
        {
            return;
        }
        user.Warns.RemoveAll(x => x.ModeratorId == moderatorId);
        await collection.ReplaceOneAsync(x => x.Id == user.Id, user);
    }*/
    
    public async Task<int> UpdateUserPoints(ulong userId, int pointsToAdd = 0, int newPoints = 0)
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

        if (newPoints is not 0)
        {
            user.Points = newPoints;
        }
        else
        {
            user.Points += pointsToAdd;
        }
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

    public async Task<int> UpdateUserLevel(ulong userId, int levelsToAdd)
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
    
    public async Task<int> GetUserLevelById(ulong userId)
    {
        var client = new MongoClient(_config.MongoDb.ConnectionString);
        var database = client.GetDatabase(_config.MongoDb.Database);
        var collection = database.GetCollection<UserModel>(_config.MongoDb.Collection);

        var user = (await collection.FindAsync(x => x.UserId == userId)).ToList().FirstOrDefault();
        return user?.Level ?? 0;
    }
}