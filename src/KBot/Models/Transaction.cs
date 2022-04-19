using System;
using KBot.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class Transaction
{
    public Transaction(string id, TransactionType source, int amount, string description, string from, string to)
    {
        Id = id;
        Source = source;
        Amount = amount;
        Description = description;
        From = from;
        To = to;
        Date = DateTime.UtcNow;
    }
    public Transaction(string id, TransactionType source, int amount, string description = null)
    {
        Id = id;
        Source = source;
        Amount = amount;
        Description = description;
        From = null;
        To = null;
        Date = DateTime.UtcNow;
    }

    [BsonId] public string Id { get; set; }
    [BsonElement("type")] public TransactionType Source { get; set; }
    [BsonElement("amount")] public int Amount { get; set; }
    [BsonElement("date")] public DateTime Date { get; set; }
    [BsonElement("desc")] public string Description { get; set; }
    [BsonElement("from")] public string From { get; set; }
    [BsonElement("to")] public string To { get; set; }

    public override string ToString()
    {
        return $"`ID: {Id}` `Date: {Date.ToString("yyyy.MM.dd")}` `Amount: {Amount}`";
    }
}