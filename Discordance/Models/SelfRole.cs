namespace Discordance.Models;

#pragma warning disable CS8618
public class SelfRole
{
    public SelfRole(ulong roleId, string title, string emote, string? description)
    {
        RoleId = roleId;
        Title = title;
        Emote = emote;
        Description = description;
    }

    public ulong RoleId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Emote { get; set; }
}