using System.Threading.Tasks;
using Discord.WebSocket;
using KBot.Config;

namespace KBot.Modules;

public class Announcements
{
    private static DiscordSocketClient _client;
    private static ConfigModel.Config _config;
    private static ulong UserAnnouncementChannelId;
    
    public Announcements(DiscordSocketClient client, ConfigModel.Config config)
    {
        _client = client;
        _config = config;
    }

    public Task InitializeAsync()
    {
        UserAnnouncementChannelId = _config.Announcements.UserAnnouncementChannelId;
        _client.UserJoined += AnnounceUserJoined;
        _client.UserLeft += AnnounceUserLeft;
        _client.UserBanned += AnnounceUserBanned;
        _client.UserUnbanned += AnnounceUserUnbanned;
        return Task.CompletedTask;
    }
    
    private static async Task AnnounceUserJoined(SocketGuildUser user)
    {
        var channel = user.Guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":wave: Üdv a szerveren {user.Mention}, érezd jól magad!");
    }
    
    private static async Task AnnounceUserLeft(SocketGuild guild, SocketUser user)
    {
        var channel = guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":cry: {user.Mention} elhagyta a szervert.");
    }

    private static async Task AnnounceUserBanned(SocketUser user, SocketGuild guild)
    {
        var channel = guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":no_entry: {user.Mention} ki lett tiltva a szerverről.");
    }

    private static async Task AnnounceUserUnbanned(SocketUser user, SocketGuild guild)
    {
        var channel = guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":grinning: {user.Mention} kitiltása vissza lett vonva.");
    }
}