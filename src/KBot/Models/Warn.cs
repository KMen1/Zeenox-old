using System;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class Warn
{
    public Warn(
        string id,
        ulong guildId,
        ulong givenById,
        ulong givenToId,
        string reason,
        DateTime date
    )
    {
        Id = id;
        GuildId = guildId;
        GivenById = givenById;
        GivenToId = givenToId;
        Reason = reason;
        Date = date;
    }

    [BsonId]
    public string Id { get; set; }

    [BsonElement("guildid")]
    public ulong GuildId { get; set; }

    [BsonElement("given_by_id")]
    public ulong GivenById { get; set; }

    [BsonElement("give_to_id")]
    public ulong GivenToId { get; set; }

    [BsonElement("reason")]
    public string Reason { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; }
}
