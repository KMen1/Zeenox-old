using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using KBot.Services;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KBot.Modules.Announcements;

public class AnnouncementsModule : DiscordClientService
{
    private static DatabaseService _database;

    public AnnouncementsModule(DiscordSocketClient client, ILogger<AnnouncementsModule> logger, DatabaseService database) : base(client, logger)
    {
        _database = database;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.UserJoined += AnnounceUserJoinedAsync;
        Client.UserLeft += AnnounceUserLeftAsync;
        Client.UserBanned += AnnounceUserBannedAsync;
        Client.UserUnbanned += AnnounceUserUnbannedAsync;
        Log.Logger.Information("Guild Announcements Module Loaded");
        return Task.CompletedTask;
    }

    private static async Task AnnounceUserJoinedAsync(SocketGuildUser user)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }

        var config = await _database.GetGuildConfigAsync(user.Guild.Id).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = user.Guild.GetTextChannel(config.Announcements.UserJoinedChannelId);
        await user.AddRoleAsync(config.Announcements.JoinRoleId).ConfigureAwait(false);
        await channel.SendMessageAsync($":wave: Üdv a szerveren {user.Mention}, érezd jól magad!").ConfigureAwait(false);
    }

    private static async Task AnnounceUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = guild.GetTextChannel(config.Announcements.UserLeftChannelId);
        await channel.SendMessageAsync($":cry: {user.Mention} elhagyta a szervert.").ConfigureAwait(false);
    }

    private static async Task AnnounceUserBannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = guild.GetTextChannel(config.Announcements.UserBannedChannelId);
        await channel.SendMessageAsync($":no_entry: {user.Mention} ki lett tiltva a szerverről.").ConfigureAwait(false);
    }

    private static async Task AnnounceUserUnbannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = guild.GetTextChannel(config.Announcements.UserUnbannedChannelId);
        await channel.SendMessageAsync($":grinning: {user.Mention} kitiltása vissza lett vonva.").ConfigureAwait(false);
    }
}