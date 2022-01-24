using System.Threading.Tasks;
using Discord.WebSocket;
using KBot.Config;

namespace KBot.Modules.Announcements;

public class AnnouncementsModule
{
    private static DiscordSocketClient _client;
    private static ConfigModel.Config _config;
    private static ulong _userAnnouncementChannelId;

    public AnnouncementsModule(DiscordSocketClient client, ConfigModel.Config config)
    {
        _client = client;
        _config = config;
    }

    public void Initialize()
    {
        _userAnnouncementChannelId = _config.Announcements.UserAnnouncementChannelId;
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
        var channel = user.Guild.GetTextChannel(_userAnnouncementChannelId);
        await channel.SendMessageAsync($":wave: Üdv a szerveren {user.Mention}, érezd jól magad!").ConfigureAwait(false);
    }

    private static async Task AnnounceUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var channel = guild.GetTextChannel(_userAnnouncementChannelId);
        await channel.SendMessageAsync($":cry: {user.Mention} elhagyta a szervert.").ConfigureAwait(false);
    }

    private static async Task AnnounceUserBannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var channel = guild.GetTextChannel(_userAnnouncementChannelId);
        await channel.SendMessageAsync($":no_entry: {user.Mention} ki lett tiltva a szerverről.").ConfigureAwait(false);
    }

    private static async Task AnnounceUserUnbannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var channel = guild.GetTextChannel(_userAnnouncementChannelId);
        await channel.SendMessageAsync($":grinning: {user.Mention} kitiltása vissza lett vonva.").ConfigureAwait(false);
    }
}