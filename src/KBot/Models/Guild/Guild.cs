using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models.Guild;

public class Guild
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)] 
    public string DocId { get; set; }
    [BsonElement("guildid")] public ulong Id { get; set; }
    [BsonElement("config")] public GuildConfig Config { get; set; }
    [BsonElement("users")] public List<User.User> Users { get; set; }
    [BsonElement("buttonroles")] public List<ButtonRoleMessage> ButtonRoles { get; set; }

    public Guild(List<SocketGuildUser> users)
    {
        Id = users[0].Guild.Id;
        Config = new GuildConfig();
        Users = new List<User.User>();
        ButtonRoles = new List<ButtonRoleMessage>();
        users.ConvertAll(x => new User.User(x)).ForEach(x => Users.Add(x));
    }
}
public class ButtonRoleMessage
{
    [BsonElement("title")] public string Title { get; set; }
    [BsonElement("description")] public string Description { get; set; }
    [BsonElement("roles")] public List<ButtonRole> Roles { get; set; }
    [BsonElement("messageid")] public ulong MessageId { get; set; }
    [BsonElement("channelid")] public ulong ChannelId { get; set; }

    public bool AddRole(ButtonRole role)
    {
        if (Roles.Exists(x => x.RoleId == role.RoleId))
            return false;
        Roles.Add(role);
        return true;
    }

    public bool RemoveRole(IRole role)
    {
        if (!Roles.Exists(x => x.RoleId == role.Id))
            return false;
        Roles.Remove(Roles.Find(x => x.RoleId == role.Id));
        return true;
    }
    public MessageComponent ToButtons()
    {
        var comp = new ComponentBuilder();
        foreach (var role in Roles)
        {
            var emoteResult = Emote.TryParse(role.Emote, out var emote);
            var emojiResult = Emoji.TryParse(role.Emote, out var emoji);
            if (emoteResult)
                comp.WithButton(role.Title, $"rrtr:{role.RoleId}", emote: emote);
            else if (emojiResult)
                comp.WithButton(role.Title, $"rrtr:{role.RoleId}", emote: emoji);
            else
                comp.WithButton(role.Title, $"rrtr:{role.RoleId}");
        }
        return comp.Build();
    }

    public ButtonRoleMessage(ulong channelId, ulong messageId, string title, string description)
    {
        ChannelId = channelId;
        MessageId = messageId;
        Title = title;
        Description = description;
        Roles = new List<ButtonRole>();
    }
}
public class ButtonRole
{
    public ButtonRole(ulong roleId, string title, string emote)
    {
        RoleId = roleId;
        Title = title;
        Emote = emote;
    }
    
    [BsonElement("emote")] public string Emote { get; private set; }
    [BsonElement("title")] public string Title { get; private set; }
    [BsonElement("roleid")] public ulong RoleId { get; private set; }
}
public class LevelRole
{
    [BsonElement("roleid")] public ulong Id { get; }
    [BsonElement("level")] public int Level { get; }
    
    public LevelRole(ulong id, int level)
    {
        Level = level;
        Id = id;
    }
}
