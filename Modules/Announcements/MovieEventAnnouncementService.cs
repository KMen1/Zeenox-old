using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;
using KBot.Enums;
using KBot.Helpers;

namespace KBot.Modules.Announcements;

public class MovieModule
{
    private readonly DiscordSocketClient _client;
    private readonly ConfigModel.Config _config;
    private static ulong MovieRoleId;
    private static ulong MovieStreamingChannelId;
    private static ulong MovieEventAnnouncementChannelId;

    public MovieModule(DiscordSocketClient client, ConfigModel.Config config)
    {
        _client = client;
        _config = config;
    }

    public void Initialize()
    {
        _client.GuildScheduledEventCreated += AnnounceScheduledEventCreatedAsync;
        _client.GuildScheduledEventUpdated += AnnounceScheduledEventUpdatedAsync;
        _client.GuildScheduledEventStarted += AnnounceScheduledEventStartedAsync;
        _client.GuildScheduledEventCancelled += AnnounceScheduledEventCancelledAsync;

        MovieRoleId = _config.Movie.RoleId;
        MovieStreamingChannelId = _config.Movie.StreamingChannelId;
        MovieEventAnnouncementChannelId = _config.Movie.EventAnnouncementChannelId;
    }

    private static async Task AnnounceScheduledEventCreatedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: await EmbedHelper.MovieEventEmbed(arg, EventEmbedType.Scheduled).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }

    private static async Task AnnounceScheduledEventUpdatedAsync(Cacheable<SocketGuildEvent, ulong> arg1, SocketGuildEvent arg2)
    {
        var eventChannel = arg2.Channel;

        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg2.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg2.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: await EmbedHelper.MovieEventEmbed(arg2, EventEmbedType.Updated).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }

    private static async Task AnnounceScheduledEventStartedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;

        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: await EmbedHelper.MovieEventEmbed(arg, EventEmbedType.Started).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }

    private static async Task AnnounceScheduledEventCancelledAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;

        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: await EmbedHelper.MovieEventEmbed(arg, EventEmbedType.Cancelled).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }
}