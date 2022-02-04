using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;
using KBot.Modules.Announcements.Helpers;

namespace KBot.Modules.Announcements;

public class MovieModule
{
    private readonly DiscordSocketClient _client;
    private static DatabaseService _database;

    public MovieModule(DiscordSocketClient client, DatabaseService database)
    {
        _client = client;
        _database = database;
    }

    public void Initialize()
    {
        _client.GuildScheduledEventCreated += AnnounceScheduledEventCreatedAsync;
        _client.GuildScheduledEventUpdated += AnnounceScheduledEventUpdatedAsync;
        _client.GuildScheduledEventStarted += AnnounceScheduledEventStartedAsync;
        _client.GuildScheduledEventCancelled += AnnounceScheduledEventCancelledAsync;
    }

    private static async Task AnnounceScheduledEventCreatedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        var config = await _database.GetGuildConfigAsync(arg.Guild.Id).ConfigureAwait(false);
        var streamingChannelId = config.MovieEvents.StreamingChannelId;
        var movieRoleId = config.MovieEvents.RoleId;
        var movieEventAnnouncementChannelId = config.MovieEvents.AnnouncementChannelId;
        if (eventChannel is not null && eventChannel.Id == streamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(movieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(movieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: Embeds.MovieEventEmbed(arg, EventEmbedType.Scheduled))
                .ConfigureAwait(false);
        }
    }

    private static async Task AnnounceScheduledEventUpdatedAsync(Cacheable<SocketGuildEvent, ulong> arg1, SocketGuildEvent arg2)
    {
        var eventChannel = arg2.Channel;
        var config = await _database.GetGuildConfigAsync(arg2.Guild.Id).ConfigureAwait(false);
        var streamingChannelId = config.MovieEvents.StreamingChannelId;
        var movieRoleId = config.MovieEvents.RoleId;
        var movieEventAnnouncementChannelId = config.MovieEvents.AnnouncementChannelId;
        if (eventChannel is not null && eventChannel.Id == streamingChannelId)
        {
            var movieRole = arg2.Guild.GetRole(movieRoleId);
            var notifyChannel = arg2.Guild.GetTextChannel(movieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: Embeds.MovieEventEmbed(arg2, EventEmbedType.Updated))
                .ConfigureAwait(false);
        }
    }

    private static async Task AnnounceScheduledEventStartedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        var config = await _database.GetGuildConfigAsync(arg.Guild.Id).ConfigureAwait(false);
        var streamingChannelId = config.MovieEvents.StreamingChannelId;
        var movieRoleId = config.MovieEvents.RoleId;
        var movieEventAnnouncementChannelId = config.MovieEvents.AnnouncementChannelId;
        if (eventChannel is not null && eventChannel.Id == streamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(movieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(movieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: Embeds.MovieEventEmbed(arg, EventEmbedType.Started))
                .ConfigureAwait(false);
        }
    }

    private static async Task AnnounceScheduledEventCancelledAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        var config = await _database.GetGuildConfigAsync(arg.Guild.Id).ConfigureAwait(false);
        var streamingChannelId = config.MovieEvents.StreamingChannelId;
        var movieRoleId = config.MovieEvents.RoleId;
        var movieEventAnnouncementChannelId = config.MovieEvents.AnnouncementChannelId;
        if (eventChannel is not null && eventChannel.Id == streamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(movieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(movieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: Embeds.MovieEventEmbed(arg, EventEmbedType.Cancelled))
                .ConfigureAwait(false);
        }
    }
}