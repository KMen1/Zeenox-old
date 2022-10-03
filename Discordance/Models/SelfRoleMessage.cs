using System.Collections.Generic;
using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace Discordance.Models;

public class SelfRoleMessage
{
    public SelfRoleMessage(ulong channelId, ulong messageId, string title, string description)
    {
        ChannelId = channelId;
        MessageId = messageId;
        Title = title;
        Description = description;
        Roles = new List<SelfRole>();
    }

    public ulong ChannelId { get; set; }

    [BsonId] public ulong MessageId { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
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