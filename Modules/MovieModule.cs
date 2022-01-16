using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;
using KBot.Enums;
using KBot.Helpers;

namespace KBot.Modules;

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

    public Task InitializeAsync()
    {
        _client.GuildScheduledEventCreated += AnnounceScheduledEventCreated;
        _client.GuildScheduledEventUpdated += AnnounceScheduledEventUpdated;
        _client.GuildScheduledEventStarted += AnnounceScheduledEventStarted;
        _client.GuildScheduledEventCancelled += AnnounceScheduledEventCancelled;
        
        MovieRoleId = _config.Movie.RoleId;
        MovieStreamingChannelId = _config.Movie.StreamingChannelId;
        MovieEventAnnouncementChannelId = _config.Movie.EventAnnouncementChannelId;

        return Task.CompletedTask;
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
    }
    
    private static async Task AnnounceScheduledEventUpdated(Cacheable<SocketGuildEvent, ulong> arg1, SocketGuildEvent arg2)
    {
        var eventChannel = arg2.Channel;
        
        if (eventChannel is not null && eventChannel.Id == MovieStreamingChannelId)
        {
            var movieRole = arg2.Guild.GetRole(MovieRoleId);
            var notifyChannel = arg2.Guild.GetTextChannel(MovieEventAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(movieRole.Mention, embed: await EmbedHelper.MovieEventEmbed(arg2, EventEmbedType.Updated));
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
    }
}