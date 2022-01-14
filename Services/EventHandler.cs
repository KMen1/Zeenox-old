using System.Threading.Tasks;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Helpers;

namespace KBot.Services;

public class EventHandler
{
    private readonly DiscordSocketClient _client;

    private static ulong UserAnnouncementChannelId;
    private static ulong TourAnnouncementChannelId;
    private static ulong MovieEventAnnouncementChannelId;
    private static ulong MovieStreamingChannelId;
    
    private static ulong MovieRoleId;
    private static ulong TourRoleId;
    
    public EventHandler(DiscordSocketClient client, ConfigService config)
    {
        _client = client;
        
        UserAnnouncementChannelId = config.UserAnnouncementChannelId;
        TourAnnouncementChannelId = config.TourAnnouncementChannelId;
        MovieEventAnnouncementChannelId = config.MovieEventAnnouncementChannelId;
        MovieStreamingChannelId = config.MovieStreamingChannelId;
        
        MovieRoleId = config.MovieRoleId;
        TourRoleId = config.TourRoleId;
    }

    public void InitializeAsync()
    {
        _client.UserJoined += AnnounceUserJoined;
        _client.UserLeft += AnnounceUserLeft;
        _client.UserBanned += AnnounceUserBanned;
        _client.UserUnbanned += AnnounceUserUnbanned;
        _client.GuildScheduledEventCreated += AnnounceScheduledEventCreated;
        _client.GuildScheduledEventStarted += AnnounceScheduledEventStarted;
        _client.GuildScheduledEventCancelled += AnnounceScheduledEventCancelled;
    }
    
    private static async Task AnnounceScheduledEventCreated(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        
        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention, embed: await EmbedHelper.MovieEventEmbed(arg, EventEmbedType.Scheduled));
        }
        else if (arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(TourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(TourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Scheduled));
        }
    }
    
    private static async Task AnnounceScheduledEventStarted(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        
        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention, embed: await EmbedHelper.MovieEventEmbed(arg, EventEmbedType.Started));
        }
        else if (arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(TourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(TourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Started));
        }
    }
    
    private static async Task AnnounceScheduledEventCancelled(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;

        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention, embed: await EmbedHelper.MovieEventEmbed(arg, EventEmbedType.Cancelled));
        }
        else if (arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(TourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(TourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Cancelled));
        }
    }

    private static async Task AnnounceUserJoined(SocketGuildUser user)
    {
        var channel = user.Guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":wave: Üdv a szerveren {user.Mention}, érezd jól magad!");
    }
    
    private static async Task AnnounceUserLeft(SocketGuild guild, SocketUser user)
    {
        var channel = guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":cry: {user.Mention} elhagyta a szervert.");
    }

    private static async Task AnnounceUserBanned(SocketUser user, SocketGuild guild)
    {
        var channel = guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":no_entry: {user.Mention} ki lett tiltva a szerverről.");
    }

    private static async Task AnnounceUserUnbanned(SocketUser user, SocketGuild guild)
    {
        var channel = guild.GetTextChannel(UserAnnouncementChannelId);
        await channel.SendMessageAsync($":grinning: {user.Mention} kitiltása vissza lett vonva.");
    }
}