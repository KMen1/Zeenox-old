#pragma warning disable MA0048
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Config;

[RequireUserPermission(GuildPermission.Administrator)]
[Group("announcements", "Setup announcements for your server")]
public class Announcements : SlashModuleBase
{
    [SlashCommand("join", "Set the join message channel")]
    public async Task SetJoinAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.WelcomeChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(channel is null ? "Welcome messages disabled!" :"Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("autorole", "Set the auto-role for new members")]
    public async Task SetAutoRoleAsync(IRole? role = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.WelcomeRoleId = role?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(role is null ? "Auto role disabled!" : "Role set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("leave", "Set the leave message channel")]
    public async Task SetLeaveAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LeaveChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(channel is null ? "Goodbye messages disabled!" : "Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("ban", "Set the ban message channel")]
    public async Task SetBanAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.BanChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(channel is null ? "Ban messages disabled!" : "Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("unban", "Set the unban message channel")]
    public async Task SetUnbanAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.UnbanChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(channel is null ? "Unban messages disabled!" : "Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}

[RequireUserPermission(GuildPermission.Administrator)]
[Group("temporaryvoice", "Setup temporary voice channels")]
public class TemporaryVoice : SlashModuleBase
{
    [SlashCommand("category", "Set the category for temporary voice channels")]
    public async Task SetCategoryAsync(ICategoryChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoiceCategoryId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(channel is null ? "Temporary voice channels disabled!" : "Category set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("channel", "Set the channel for creating temporary voice channels")]
    public async Task SetChannelAsync(IVoiceChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoiceCreateId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        await RespondAsync(channel is null ? "Temporary voice channels disabled!" : "Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}