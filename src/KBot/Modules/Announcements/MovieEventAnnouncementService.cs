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

public class MovieModule : DiscordClientService
{
    private static DatabaseService _database;

    public MovieModule(DiscordSocketClient client, ILogger<MovieModule> logger, DatabaseService database) : base(client, logger)
    {
        _database = database;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.GuildScheduledEventCreated += AnnounceScheduledEventCreatedAsync;
        Client.GuildScheduledEventUpdated += AnnounceScheduledEventUpdatedAsync;
        Client.GuildScheduledEventStarted += AnnounceScheduledEventStartedAsync;
        Client.GuildScheduledEventCancelled += AnnounceScheduledEventCancelledAsync;
        Log.Logger.Information("Movie Events Module Loaded");
        return Task.CompletedTask;
    }

    private static async Task AnnounceScheduledEventCreatedAsync(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        var config = await _database.GetGuildConfigFromCacheAsync(arg.Guild.Id).ConfigureAwait(false);
        if (!config.MovieEvents.Enabled)
        {
            return;
        }
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
        var config = await _database.GetGuildConfigFromCacheAsync(arg2.Guild.Id).ConfigureAwait(false);
        if (!config.MovieEvents.Enabled)
        {
            return;
        }
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
        var config = await _database.GetGuildConfigFromCacheAsync(arg.Guild.Id).ConfigureAwait(false);
        if (!config.MovieEvents.Enabled)
        {
            return;
        }
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
        var config = await _database.GetGuildConfigFromCacheAsync(arg.Guild.Id).ConfigureAwait(false);
        if (!config.MovieEvents.Enabled)
        {
            return;
        }
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