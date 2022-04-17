using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models.Guild;

namespace KBot.Modules.Config;

[RequireUserPermission(GuildPermission.Administrator)]
[Group("announcements", "Setup announcements for your server")]
public class Announcements : SlashModuleBase
{
    [SlashCommand("join", "Set the join message channel")]
    public async Task SetJoinAsync(ITextChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.JoinChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("autorole", "Set the auto-role for new members")]
    public async Task SetAutoRoleAsync(IRole role)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.JoinRoleId = role.Id)
            .ConfigureAwait(false);
        await RespondAsync("Role set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("leave", "Set the leave message channel")]
    public async Task SetLeaveAsync(ITextChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.LeftChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("ban", "Set the ban message channel")]
    public async Task SetBanAsync(ITextChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.BanChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("unban", "Set the unban message channel")]
    public async Task SetUnbanAsync(ITextChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.Announcements.UnbanChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}

[RequireUserPermission(GuildPermission.Administrator)]
[Group("temporaryvoice", "Setup temporary voice channels")]
public class TemporaryVoice : SlashModuleBase
{
    [SlashCommand("enable", "Toggle temporary voice channels for this server")]
    public async Task EnableTemporaryVoiceAsync(bool setting)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoice.Enabled = setting)
            .ConfigureAwait(false);
        await RespondAsync(setting ? "Temporary voice channels enabled!" : "Temporary voice channels disabled!",
            ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("category", "Set the category for temporary voice channels")]
    public async Task SetCategoryAsync(ICategoryChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoice.CategoryId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Category set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("channel", "Set the channel for creating temporary voice channels")]
    public async Task SetChannelAsync(IVoiceChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.TemporaryVoice.CreateChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}

[RequireUserPermission(GuildPermission.Administrator)]
[Group("movieevents", "Setup movie events")]
public class MovieEvents : SlashModuleBase
{
    [SlashCommand("enable", "Toggle movie events for this server")]
    public async Task EnableMovieEventsAsync(bool setting)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.Enabled = setting)
            .ConfigureAwait(false);
        await RespondAsync(setting ? "Movie Events Enabled!" : "Movie Events Disabled!", ephemeral: true)
            .ConfigureAwait(false);
    }

    [SlashCommand("channel", "Set the channel for movie event messages")]
    public async Task SetChannelAsync(ITextChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.AnnounceChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("role", "Set the movie role for notifications")]
    public async Task SetRoleAsync(IRole role)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.RoleId = role.Id).ConfigureAwait(false);
        await RespondAsync("Role Set!", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("streamchannel", "Set the channel for movie streaming")]
    public async Task SetStreamChannelAsync(IVoiceChannel channel)
    {
        await Database.UpdateGuildConfigAsync(Context.Guild, x => x.MovieEvents.StreamChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}