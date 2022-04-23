// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618, MA0048
using System;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class GameResult
{
    public GameResult()
    {
        
    }
    
    [BsonId] public string Id { get; set; }
    [BsonElement("user_id")] public ulong UserId { get; set; }
    [BsonElement("bet")] public int Bet { get; set; }
    [BsonElement("is_win")] public bool IsWin { get; set; }
    [BsonElement("prize")] public int? Prize { get; set; }
    [BsonElement("timestamp")] public DateTime Timestamp { get; set; }
}