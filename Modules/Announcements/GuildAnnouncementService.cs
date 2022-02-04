using System.Threading.Tasks;
using Discord.WebSocket;
using KBot.Database;
using Microsoft.Extensions.Caching.Memory;

namespace KBot.Modules.Announcements;

public class AnnouncementsModule
{
    private static DiscordSocketClient _client;
    private static DatabaseService _database;

    public AnnouncementsModule(DiscordSocketClient client, DatabaseService database)
    {
        _client = client;
        _database = database;
    }

    public void Initialize()
    {
        _client.UserJoined += AnnounceUserJoinedAsync;
        _client.UserLeft += AnnounceUserLeftAsync;
        _client.UserBanned += AnnounceUserBannedAsync;
        _client.UserUnbanned += AnnounceUserUnbannedAsync;
    }

    private static async Task AnnounceUserJoinedAsync(SocketGuildUser user)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }

        var config = await _database.GetGuildConfigFromCacheAsync(user.Guild.Id).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = user.Guild.GetTextChannel(config.Announcements.UserJoinedChannelId);
        await channel.SendMessageAsync($":wave: Üdv a szerveren {user.Mention}, érezd jól magad!").ConfigureAwait(false);
    }

    private static async Task AnnounceUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigFromCacheAsync(guild.Id).ConfigureAwait(false);
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
        var config = await _database.GetGuildConfigFromCacheAsync(guild.Id).ConfigureAwait(false);
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
        var config = await _database.GetGuildConfigFromCacheAsync(guild.Id).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = guild.GetTextChannel(config.Announcements.UserUnbannedChannelId);
        await channel.SendMessageAsync($":grinning: {user.Mention} kitiltása vissza lett vonva.").ConfigureAwait(false);
    }
}