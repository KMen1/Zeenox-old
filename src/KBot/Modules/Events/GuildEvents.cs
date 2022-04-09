using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Services;
using Serilog;

namespace KBot.Modules.Events;

public class GuildEvents : IInjectable
{
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _database;
    private readonly List<(SocketUser user, ulong channelId)> _channels;

    public GuildEvents(DiscordSocketClient client, DatabaseService database)
    {
        _client = client;
        _database = database;
        client.GuildAvailable += ClientOnGuildAvailableAsync;
        //client.GuildMemberUpdated += ClientOnGuildMemberUpdatedAsync;
        client.UserJoined += AnnounceUserJoinedAsync;
        client.UserLeft += AnnounceUserLeftAsync;
        client.UserBanned += AnnounceUserBannedAsync;
        client.UserUnbanned += AnnounceUserUnbannedAsync;
        client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        client.GuildScheduledEventCreated += guildEvent => HandleScheduledEventAsync(guildEvent, EventState.Scheduled);
        client.GuildScheduledEventUpdated += (_, after) => HandleScheduledEventAsync(after, EventState.Updated);
        client.GuildScheduledEventStarted += guildEvent => HandleScheduledEventAsync(guildEvent, EventState.Started);
        client.GuildScheduledEventCancelled += guildEvent => HandleScheduledEventAsync(guildEvent, EventState.Cancelled);
        _channels = new List<(SocketUser user, ulong channelId)>();
        Log.Logger.Information("GuildEvents Module Loaded");
    }

    /*private Task ClientOnGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
    {
        throw new System.NotImplementedException();
    }*/

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        var guild = after.VoiceChannel?.Guild ?? before.VoiceChannel?.Guild;
        if (user.IsBot)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild!).ConfigureAwait(false);
        if (!config.TemporaryVoice.Enabled)
        {
            return;
        }
        if (after.VoiceChannel is not null && after.VoiceChannel.Id == config.TemporaryVoice.CreateChannelId)
        {
            var voiceChannel = await guild!.CreateVoiceChannelAsync($"{user.Username} Társalgója", x =>
            {
                x.UserLimit = 2;
                x.CategoryId = config.TemporaryVoice.CategoryId;
                x.Bitrate = 96000;
                x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
                {
                    new Overwrite(user.Id, PermissionTarget.User,
                        new OverwritePermissions(connect: PermValue.Allow, manageRoles: PermValue.Allow, moveMembers: PermValue.Allow)),
                    new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role,
                        new OverwritePermissions(connect: PermValue.Allow)),
                    new Overwrite(_client.CurrentUser.Id, PermissionTarget.User,
                        new OverwritePermissions(viewChannel: PermValue.Allow, manageChannel: PermValue.Allow,
                            connect: PermValue.Allow, moveMembers: PermValue.Allow))
                });
            }).ConfigureAwait(false);
            await guild.GetUser(user.Id).ModifyAsync(x => x.Channel = voiceChannel).ConfigureAwait(false);
            _channels.Add((user, voiceChannel.Id));
        }
        else if (_channels.Contains((user, before.VoiceChannel?.Id ?? 0)))
        {
            var (puser, channelId) = _channels.First(x => x.user == user && x.channelId == before.VoiceChannel.Id);
            await guild!.GetVoiceChannel(channelId).DeleteAsync().ConfigureAwait(false);
            _channels.Remove((puser, channelId));
        }
    }

    private async Task ClientOnGuildAvailableAsync(SocketGuild guild)
    {
        if (!await _database.CheckIfGuildIsInDbAsync(guild).ConfigureAwait(false))
        {
            var users = _client.GetGuild(guild.Id).Users.ToList();
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

        var streamingChannelId = config.MovieEvents.StreamChannelId;
        var movieRoleId = config.MovieEvents.RoleId;
        var movieEventAnnouncementChannelId = config.MovieEvents.AnnounceChannelId;
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
        var tourEventAnnouncementChannelId = config.TourEvents.AnnounceChannelId;
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
        var dbUser = await _database.GetUserAsync(user.Guild, user).ConfigureAwait(false);
        var config = await _database.GetGuildConfigAsync(user.Guild).ConfigureAwait(false);
        if (dbUser is not null)
        {
            foreach (var roleId in dbUser.Roles)
            {
                var guild = user.Guild;
                var role = guild.GetRole(roleId);
                if (role is null)
                {
                    continue;
                }
                await user.AddRoleAsync(role).ConfigureAwait(false);
            }
        }
        else
        {
            await _database.AddUserAsync(user.Guild, user).ConfigureAwait(false);
        }
        await user.AddRoleAsync(config.Announcements.JoinRoleId).ConfigureAwait(false);

        if (!config.Announcements.Enabled)
        {
            return;
        }
        var channel = user.Guild.GetTextChannel(config.Announcements.JoinChannelId);
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
        var channel = guild.GetTextChannel(config.Announcements.LeftChannelId);
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
        var channel = guild.GetTextChannel(config.Announcements.BanChannelId);
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
        var channel = guild.GetTextChannel(config.Announcements.UnbanChannelId);
        await channel.SendMessageAsync($":grinning: {user.Mention} kitiltása vissza lett vonva.").ConfigureAwait(false);
    }
}