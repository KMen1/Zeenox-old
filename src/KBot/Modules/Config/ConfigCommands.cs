using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Config;

[RequireUserPermission(GuildPermission.Administrator)]
[Group("announcements", "Setup announcements for your server")]
public class Announcements : SlashModuleBase
{
    [SlashCommand("join", "Set the join message channel")]
    public async Task SetJoinAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.WelcomeChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("autorole", "Set the auto-role for new members")]
    public async Task SetAutoRoleAsync(IRole role)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.WelcomeRoleId = role.Id)
            .ConfigureAwait(false);
        await RespondAsync("Role set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("leave", "Set the leave message channel")]
    public async Task SetLeaveAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LeaveChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("ban", "Set the ban message channel")]
    public async Task SetBanAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.BanChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("unban", "Set the unban message channel")]
    public async Task SetUnbanAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.UnbanChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}

[RequireUserPermission(GuildPermission.Administrator)]
[Group("temporaryvoice", "Setup temporary voice channels")]
public class TemporaryVoice : SlashModuleBase
{
    [SlashCommand("category", "Set the category for temporary voice channels")]
    public async Task SetCategoryAsync(ICategoryChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoiceCategoryId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Category set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("channel", "Set the channel for creating temporary voice channels")]
    public async Task SetChannelAsync(IVoiceChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoiceCreateId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}