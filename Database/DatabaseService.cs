using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KBot.Services;
using MongoDB.Driver;

namespace KBot.Database;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly string _warnCollectionName;
    
    public DatabaseService(ConfigService config)
    {
        _connectionString = config.MongoDbConnectionString;
        _databaseName = config.MongoDbDatabaseName;
        _warnCollectionName = config.MongoDbWarnCollectionName;
    }

    public async Task<List<WarnObject>> GetWarnsByUserId(ulong userId)
    {
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        var collection = database.GetCollection<UserModel>(_warnCollectionName);
        
        var user = await collection.FindAsync(x => x.UserId == userId);

        var warns = user.ToList().FirstOrDefault()?.Warns;
        return warns;
    }

    public async Task AddWarnToUser(ulong userId, ulong moderatorId, string reason)
    {
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        var collection = database.GetCollection<UserModel>( _warnCollectionName);

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
}