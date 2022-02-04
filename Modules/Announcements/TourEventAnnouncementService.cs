using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;
using KBot.Modules.Announcements.Helpers;

namespace KBot.Modules.Announcements;

public class TourModule
{
    private readonly DiscordSocketClient _client;
    private static DatabaseService _database;

    public TourModule(DiscordSocketClient client, DatabaseService database)
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
        var tourRoleId = config.TourEvents.RoleId;
        var tourEventAnnouncementChannelId = config.TourEvents.AnnouncementChannelId;
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(tourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(tourEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                    embed: Embeds.TourEventEmbed(arg, EventEmbedType.Scheduled)).ConfigureAwait(false);
        }
    }
    private static async Task AnnounceScheduledEventUpdatedAsync(Cacheable<SocketGuildEvent, ulong> arg1, SocketGuildEvent arg2)
    {
        var eventChannel = arg2.Channel;
        var config = await _database.GetGuildConfigAsync(arg2.Guild.Id).ConfigureAwait(false);
        var tourRoleId = config.TourEvents.RoleId;
        var tourEventAnnouncementChannelId = config.TourEvents.AnnouncementChannelId;
        if (eventChannel is null && arg2.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg2.Guild.GetRole(tourRoleId);
            var notifyChannel = arg2.Guild.GetTextChannel(tourEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                    embed: Embeds.TourEventEmbed(arg2, EventEmbedType.Updated)).ConfigureAwait(false);
        }
    }
    private static async Task AnnounceScheduledEventStartedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        var config = await _database.GetGuildConfigAsync(arg.Guild.Id).ConfigureAwait(false);
        var tourRoleId = config.TourEvents.RoleId;
        var tourEventAnnouncementChannelId = config.TourEvents.AnnouncementChannelId;
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(tourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(tourEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                    embed: Embeds.TourEventEmbed(arg, EventEmbedType.Started)).ConfigureAwait(false);
        }
    }
    private static async Task AnnounceScheduledEventCancelledAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        var config = await _database.GetGuildConfigAsync(arg.Guild.Id).ConfigureAwait(false);
        var tourRoleId = config.TourEvents.RoleId;
        var tourEventAnnouncementChannelId = config.TourEvents.AnnouncementChannelId;
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(tourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(tourEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                    embed: Embeds.TourEventEmbed(arg, EventEmbedType.Cancelled)).ConfigureAwait(false);
        }
    }
}