﻿using System;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class GameResult
{
    public GameResult()
    {
        
    }
    
    [BsonElement("game_id")] public string GameId { get; set; }
    [BsonElement("user_id")] public ulong UserId { get; set; }
    [BsonElement("bet")] public int Bet { get; set; }
    [BsonElement("is_win")] public bool IsWin { get; set; }
    [BsonElement("prize")] public int? Prize { get; set; }
    [BsonElement("timestamp")] public DateTime Timestamp { get; set; }
}