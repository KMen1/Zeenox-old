using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Modules.Announcements.Helpers;
using KBot.Services;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KBot.Modules.Announcements;

public class TourModule : DiscordClientService
{
    private static DatabaseService _database;

    public TourModule(DiscordSocketClient client, ILogger<TourModule> logger, DatabaseService database) : base(client, logger)
    {
        _database = database;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.GuildScheduledEventCreated += AnnounceScheduledEventCreatedAsync;
        Client.GuildScheduledEventUpdated += AnnounceScheduledEventUpdatedAsync;
        Client.GuildScheduledEventStarted += AnnounceScheduledEventStartedAsync;
        Client.GuildScheduledEventCancelled += AnnounceScheduledEventCancelledAsync;
        Log.Logger.Information("Tour Events Module Loaded");
        return Task.CompletedTask;
    }
    
    private static async Task AnnounceScheduledEventCreatedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        var config = await _database.GetGuildConfigFromCacheAsync(arg.Guild.Id).ConfigureAwait(false);
        if (!config.TourEvents.Enabled)
        {
            return;
        }
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
        var config = await _database.GetGuildConfigFromCacheAsync(arg2.Guild.Id).ConfigureAwait(false);
        if (!config.TourEvents.Enabled)
        {
            return;
        }
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
        var config = await _database.GetGuildConfigFromCacheAsync(arg.Guild.Id).ConfigureAwait(false);
        if (!config.TourEvents.Enabled)
        {
            return;
        }
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
        var config = await _database.GetGuildConfigFromCacheAsync(arg.Guild.Id).ConfigureAwait(false);
        if (!config.TourEvents.Enabled)
        {
            return;
        }
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