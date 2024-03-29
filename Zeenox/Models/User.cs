﻿using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Zeenox.Models;

public class User
{
    public User(ulong id)
    {
        Id = id;
        Balance = 10000;
        LastDailyCreditClaim = null;
        Wins = 0;
        Losses = 0;
        MoneyWon = 0;
        MoneyLost = 0;
        Playlists = new List<Playlist> {new("Favorites")};
    }

    [BsonId] public ulong Id { get; set; }
    public int Balance { get; set; }
    public DateTime? LastDailyCreditClaim { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MoneyWon { get; set; }
    public int MoneyLost { get; set; }
    public List<Playlist> Playlists { get; set; }

    [BsonIgnore] public int GamesPlayed => Wins + Losses;
    [BsonIgnore] public int GambleLevel => GamesPlayed / 10;
    [BsonIgnore] public double WinRate => Math.Round(Wins / (double) GamesPlayed * 100, 2);
    [BsonIgnore] public int MinimumBet => (int) Math.Round(Math.Pow(GambleLevel, 2.99996) + 185);

    public bool CanStartGame(int bet)
    {
        return bet >= MinimumBet && bet <= Balance;
    }
}

public class Playlist
{
    public Playlist(string name)
    {
        Name = name;
        Songs = new List<string>();
    }

    public string Name { get; set; }
    public List<string> Songs { get; set; }
}