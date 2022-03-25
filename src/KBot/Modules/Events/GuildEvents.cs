using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Services;
using Serilog;

namespace KBot.Modules.Events;

public class GuildEvents
{
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _database;

    public GuildEvents(DiscordSocketClient client, DatabaseService database)
    {
        _client = client;
        _database = database;
        client.GuildAvailable += ClientOnGuildAvailableAsync;
        client.UserJoined += AnnounceUserJoinedAsync;
        client.UserLeft += AnnounceUserLeftAsync;
        client.UserBanned += AnnounceUserBannedAsync;
        client.UserUnbanned += AnnounceUserUnbannedAsync;
        client.GuildScheduledEventCreated += guildEvent => HandleScheduledEventAsync(guildEvent, EventState.Scheduled);
        client.GuildScheduledEventUpdated += (_, after) => HandleScheduledEventAsync(after, EventState.Updated);
        client.GuildScheduledEventStarted += guildEvent => HandleScheduledEventAsync(guildEvent, EventState.Started);
        client.GuildScheduledEventCancelled += guildEvent => HandleScheduledEventAsync(guildEvent, EventState.Cancelled);
        Log.Logger.Information("Guild Announcements Module Loaded");
    }

    private async Task ClientOnGuildAvailableAsync(SocketGuild guild)
    {
        if (!await _database.CheckIfGuildIsInDbAsync(guild).ConfigureAwait(false))
        {
            var users = _client.GetGuild(guild.Id).Users.Where(x => !x.IsBot).ToList();
            await _database.AddGuildAsync(users).ConfigureAwait(false);
        }
    }

    private async Task HandleScheduledEventAsync(SocketGuildEvent guildEvent, EventState type)
    {
        var eventChannel = guildEvent.Channel;
        var config = await _database.GetGuildConfigAsync(guildEvent.Guild).ConfigureAwait(false);
        if (!config.MovieEvents.Enabled && !config.TourEvents.Enabled)
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
                    embed: new EmbedBuilder().MovieEventEmbed(guildEvent, type))
                .ConfigureAwait(false);
            return;
        }
        
        var tourRoleId = config.TourEvents.RoleId;
        var tourEventAnnouncementChannelId = config.TourEvents.AnnouncementChannelId;
        if (eventChannel is null && guildEvent.Location.Contains("goo.gl/maps"))
        {
            var tourRole = guildEvent.Guild.GetRole(tourRoleId);
            var notifyChannel = guildEvent.Guild.GetTextChannel(tourEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention,
                embed: new EmbedBuilder().TourEventEmbed(guildEvent, type)).ConfigureAwait(false);
        }
    }
    
    private async Task AnnounceUserJoinedAsync(SocketGuildUser user)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }

        var config = await _database.GetGuildConfigAsync(user.Guild).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var dbUser = await _database.GetUserAsync(user.Guild, user).ConfigureAwait(false);
        if (dbUser is null)
        {
            await _database.AddUserAsync(user.Guild, user).ConfigureAwait(false);
        }
        var channel = user.Guild.GetTextChannel(config.Announcements.UserJoinedChannelId);
        await user.AddRoleAsync(config.Announcements.JoinRoleId).ConfigureAwait(false);
        await channel.SendMessageAsync($":wave: Üdv a szerveren {user.Mention}, érezd jól magad!").ConfigureAwait(false);
    }

    private async Task AnnounceUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = guild.GetTextChannel(config.Announcements.UserLeftChannelId);
        await channel.SendMessageAsync($":cry: {user.Mention} elhagyta a szervert.").ConfigureAwait(false);
    }

    private async Task AnnounceUserBannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = guild.GetTextChannel(config.Announcements.UserBannedChannelId);
        await channel.SendMessageAsync($":no_entry: {user.Mention} ki lett tiltva a szerverről.").ConfigureAwait(false);
    }

    private async Task AnnounceUserUnbannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = guild.GetTextChannel(config.Announcements.UserUnbannedChannelId);
        await channel.SendMessageAsync($":grinning: {user.Mention} kitiltása vissza lett vonva.").ConfigureAwait(false);
    }
}