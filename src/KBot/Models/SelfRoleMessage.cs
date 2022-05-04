// ReSharper disable UnusedAutoPropertyAccessor.Global

#pragma warning disable CS8618, MA0048
using System.Collections.Generic;
using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace KBot.Models;

public class SelfRoleMessage
{
    public SelfRoleMessage(
        ulong guildId,
        ulong channelId,
        ulong messageId,
        string title,
        string description
    )
    {
        GuildId = guildId;
        ChannelId = channelId;
        MessageId = messageId;
        Title = title;
        Description = description;
        Roles = new List<SelfRole>();
    }

    [BsonElement("guild_id")]
    public ulong GuildId { get; set; }

    [BsonElement("channel_id")]
    public ulong ChannelId { get; set; }

    [BsonId]
    public ulong MessageId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }

    [BsonElement("roles")]
    public List<SelfRole> Roles { get; set; }

    public bool AddRole(SelfRole role)
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
        var roleItem = Roles.Find(x => x.RoleId == role.Id);
        if (roleItem is null)
            return false;
        Roles.Remove(roleItem);
        return true;
    }

    public MessageComponent ToButtons()
    {
        var comp = new ComponentBuilder();
        var select = new SelectMenuBuilder();
        select.WithCustomId("roleselect");
        select.WithMinValues(0);
        select.WithMaxValues(Roles.Count);
        foreach (var role in Roles)
        {
            var emoteResult = Emote.TryParse(role.Emote, out var emote);
            var emojiResult = Emoji.TryParse(role.Emote, out var emoji);
            if (emoteResult)
                select.AddOption(role.Title, $"{role.RoleId}", role.Description, emote);
            else if (emojiResult)
                select.AddOption(role.Title, $"{role.RoleId}", role.Description, emoji);
            else
                select.AddOption(role.Title, $"{role.RoleId}", role.Description);
        }

        comp.WithSelectMenu(select);
        return comp.Build();
    }
}

public class SelfRole
{
    public SelfRole(ulong roleId, string title, string emote, string? description)
    {
        RoleId = roleId;
        Title = title;
        Emote = emote;
        Description = description;
    }

    [BsonElement("role_id")]
    public ulong RoleId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("emote")]
    public string Emote { get; set; }
}
