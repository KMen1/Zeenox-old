using System.Threading;
using System.Threading.Tasks;
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
        Client.GuildScheduledEventCreated += guildEvent => HandleTourEventAsync(guildEvent, EventEmbedType.Scheduled);
        Client.GuildScheduledEventUpdated += (_, after) => HandleTourEventAsync(after, EventEmbedType.Scheduled);
        Client.GuildScheduledEventStarted += guildEvent => HandleTourEventAsync(guildEvent, EventEmbedType.Started);
        Client.GuildScheduledEventCancelled += guildEvent => HandleTourEventAsync(guildEvent, EventEmbedType.Cancelled);
        Log.Logger.Information("Tour Events Module Loaded");
        return Task.CompletedTask;
    }

    private static async Task HandleTourEventAsync(SocketGuildEvent guildEvent, EventEmbedType type)
    {
        var eventChannel = guildEvent.Channel;
        var config = await _database.GetGuildConfigAsync(guildEvent.Guild.Id).ConfigureAwait(false);
        if (!config.TourEvents.Enabled)
        {
            return;
        }

        var tourRoleId = config.TourEvents.RoleId;
        var tourEventAnnouncementChannelId = config.TourEvents.AnnouncementChannelId;
        if (eventChannel is null && guildEvent.Location.Contains("goo.gl/maps"))
        {
            var tourRole = guildEvent.Guild.GetRole(tourRoleId);
            var notifyChannel = guildEvent.Guild.GetTextChannel(tourEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                embed: Embeds.TourEventEmbed(guildEvent, type)).ConfigureAwait(false);
        }
    }
}