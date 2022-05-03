#pragma warning disable MA0048
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Config;

[DefaultMemberPermissions(GuildPermission.ManageGuild)]
[Group("announcements", "Setup announcements for your server")]
public class Announcements : SlashModuleBase
{
    [SlashCommand("join", "Set the join message channel")]
    public async Task SetJoinAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.WelcomeChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Welcome messages will no longer be sent**"
                : $"**Welcome messages will be sent in channel {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("autorole", "Set the auto-role for new members")]
    public async Task SetAutoRoleAsync(IRole? role = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.WelcomeRoleId = role?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(role is null ? Color.Red : Color.Green)
            .WithDescription(role is null
                ? "**New members will no longer be assigned a role when they join**"
                : $"**New members will get {role.Mention} role when they join**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("leave", "Set the leave message channel")]
    public async Task SetLeaveAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LeaveChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Goodbye messages will no longer be sent**"
                : $"**Goodbye messages will be sent in channel {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("ban", "Set the ban message channel")]
    public async Task SetBanAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.BanChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Ban messages will no longer be sent**"
                : $"**Ban messages will be sent in channel {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("unban", "Set the unban message channel")]
    public async Task SetUnbanAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.UnbanChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Unban messages will no longer be sent**"
                : $"**Unban messages will be sent in channel {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}

[DefaultMemberPermissions(GuildPermission.ManageGuild)]
[Group("temporaryvoice", "Setup temporary voice channels")]
public class TemporaryVoice : SlashModuleBase
{
    [SlashCommand("category", "Set the category for temporary voice channels")]
    public async Task SetCategoryAsync(ICategoryChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoiceCategoryId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Temporary voice channels disabled**"
                : $"**Temporary voice channel category set to {channel.Name}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("channel", "Set the channel for creating temporary voice channels")]
    public async Task SetChannelAsync(IVoiceChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoiceCreateId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Temporary voice channels disabled**"
                : $"**Temporary voice channels can now be created by joining {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}

public class SetChannelModule : SlashModuleBase
{
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("setdbd", "Sets the channel to receive weekly shrine notifications")]
    public async Task SetDbdChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.DbdNotificationChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Weekly shrine notifications are now disabled**"
                : $"**Weekly shrine notifications will be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("setepic", "Sets the channel to receive weekly epic free games.")]
    public async Task SetEpicChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.EpicNotificationChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Weekly epic notifications are now disabled**"
                : $"**Weekly epic notifications will be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("setlevel", "Set the channel for level up messages")]
    public async Task SetChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LevelUpChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Leveling system is now disabled**"
                : $"**Level up messages will be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("setafk", "Set the AFK channel")]
    public async Task SetAfkChannelAsync(IVoiceChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.AfkChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**AFK Channel disabled**"
                : $"**{channel.Mention} is set to be the AFK channel**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("addlevelrole", "Add a role to the leveling roles")]
    public async Task AddRoleAsync(IRole role, [MinValue(1)] int level)
    {
        await Mongo
            .UpdateGuildConfigAsync(Context.Guild, x => x.LevelRoles.Add(new LevelRole(role.Id, level)))
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"**{role.Mention} will now be granted after reaching level {level}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("removelevelrole", "Remove a role from the leveling roles")]
    public async Task RemoveRoleAsync(IRole role)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LevelRoles.RemoveAll(y => y.Id == role.Id))
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription($"**{role.Mention} removed from level roles**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("setsuggestion", "Set the channel for suggestion messages")]
    public async Task SetSuggestionChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.SuggestionChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(channel is null ? "Suggestions disabled!" : "Channel set!", ephemeral: true)
            .ConfigureAwait(false);
    }
}