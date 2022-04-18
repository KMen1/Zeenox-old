using System.Collections.Generic;
using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class ButtonRoleMessage
{
    public ButtonRoleMessage(ulong guildId, ulong channelId, ulong messageId, string title, string description)
    {
        GuildId = guildId;
        ChannelId = channelId;
        MessageId = messageId;
        Title = title;
        Description = description;
        Roles = new List<ButtonRole>();
    }

    [BsonElement("guild_id")] public ulong GuildId { get; set; }
    [BsonElement("channel_id")] public ulong ChannelId { get; set; }
    [BsonId] public ulong MessageId { get; set; }
    [BsonElement("title")] public string Title { get; set; }
    [BsonElement("description")] public string Description { get; set; }
    [BsonElement("roles")] public List<ButtonRole> Roles { get; set; }

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
}

public class ButtonRole
{
    public ButtonRole(ulong roleId, string title, string emote)
    {
        RoleId = roleId;
        Title = title;
        Emote = emote;
    }

    [BsonElement("role_id")] public ulong RoleId { get; }
    [BsonElement("title")] public string Title { get; }
    [BsonElement("emote")] public string Emote { get; }
}