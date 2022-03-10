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

public class MovieModule : DiscordClientService
{
    private static DatabaseService _database;

    public MovieModule(DiscordSocketClient client, ILogger<MovieModule> logger, DatabaseService database) : base(client, logger)
    {
        _database = database;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.GuildScheduledEventCreated += guildEvent => HandleMovieEventAsync(guildEvent, EventEmbedType.Scheduled);
        Client.GuildScheduledEventUpdated += (_, after) => HandleMovieEventAsync(after, EventEmbedType.Updated);
        Client.GuildScheduledEventStarted += guildEvent => HandleMovieEventAsync(guildEvent, EventEmbedType.Started);
        Client.GuildScheduledEventCancelled += guildEvent => HandleMovieEventAsync(guildEvent, EventEmbedType.Cancelled);
        Log.Logger.Information("Movie Events Module Loaded");
        return Task.CompletedTask;
    }

    private static async Task HandleMovieEventAsync(SocketGuildEvent guildEvent, EventEmbedType type)
    {
        var eventChannel = guildEvent.Channel;
        var config = await _database.GetGuildConfigAsync(guildEvent.Guild.Id).ConfigureAwait(false);
        if (!config.MovieEvents.Enabled)
        {
            return;
        }

        var streamingChannelId = config.MovieEvents.StreamingChannelId;
        var movieRoleId = config.MovieEvents.RoleId;
        var movieEventAnnouncementChannelId = config.MovieEvents.AnnouncementChannelId;
        if (eventChannel is not null && eventChannel.Id == streamingChannelId)
        {
            var movieRole = guildEvent.Guild.GetRole(movieRoleId);
            var notifyChannel = guildEvent.Guild.GetTextChannel(movieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention,
                    embed: Embeds.MovieEventEmbed(guildEvent, type))
                .ConfigureAwait(false);
        }
    }
}