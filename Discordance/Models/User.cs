using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Discordance.Models;

public class User
{
    public User(ulong id)
    {
        Id = id;
        OsuId = null;
        Balance = 10000;
        LastDailyCreditClaim = null;
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
        GuildDatas = new Dictionary<ulong, GuildData>();
    }

    [BsonId] public ulong Id { get; set; }

    public ulong? OsuId { get; set; }
    public int Balance { get; set; }
    public DateTime? LastDailyCreditClaim { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MoneyWon { get; set; }
    public int MoneyLost { get; set; }
    public Dictionary<ulong, GuildData> GuildDatas { get; set; }

    [BsonIgnore] public int GamesPlayed => Wins + Losses;

    [BsonIgnore] public int GambleLevel => GamesPlayed / 10;

    [BsonIgnore] public int GambleLevelRequired => 10 - GamesPlayed % 10;
}