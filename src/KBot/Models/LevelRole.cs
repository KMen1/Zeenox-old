// ReSharper disable UnusedAutoPropertyAccessor.Global

#pragma warning disable CS8618, MA0048
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class LevelRole
{
    public LevelRole(ulong id, int level)
    {
        Level = level;
        Id = id;
    }

    [BsonElement("role_id")] public ulong Id { get; set; }
    [BsonElement("level")] public int Level { get; set; }
}