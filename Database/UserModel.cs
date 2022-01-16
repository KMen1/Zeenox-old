using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Database;

public class UserModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    public ulong UserId { get; set; }
    
    public int Points { get; set; }
    
    public int Level { get; set; }
    
    public List<WarnObject> Warns { get; set; }
    
}

public class WarnObject
{
    public ulong ModeratorId { get; set; }
    
    public string Reason { get; set; }
    
    public DateTime Date { get; set; }
    
}