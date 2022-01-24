using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;
using KBot.Enums;
using KBot.Helpers;

namespace KBot.Modules.Announcements;

public class TourModule
{
    private readonly DiscordSocketClient _client;
    private readonly ConfigModel.Config _config;
    private static ulong _tourRoleId;
    private static ulong _tourAnnouncementChannelId;
    public TourModule(DiscordSocketClient client, ConfigModel.Config config)
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
        _tourRoleId = _config.Tour.RoleId;
        _tourAnnouncementChannelId = _config.Tour.EventAnnouncementChannelId;
    }
    private static async Task AnnounceScheduledEventCreatedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(_tourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(_tourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, 
                    embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Scheduled).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }
    private static async Task AnnounceScheduledEventUpdatedAsync(Cacheable<SocketGuildEvent, ulong> arg1, SocketGuildEvent arg2)
    {
        var eventChannel = arg2.Channel;
        if (eventChannel is null && arg2.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg2.Guild.GetRole(_tourRoleId);
            var notifyChannel = arg2.Guild.GetTextChannel(_tourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                    embed: await EmbedHelper.TourEventEmbed(arg2, EventEmbedType.Updated).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }
    private static async Task AnnounceScheduledEventStartedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(_tourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(_tourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                    embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Started).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }
    private static async Task AnnounceScheduledEventCancelledAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(_tourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(_tourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                    embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Cancelled).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }
}