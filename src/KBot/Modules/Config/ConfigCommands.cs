#pragma warning disable MA0048
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

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