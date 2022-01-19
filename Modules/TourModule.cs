using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;
using KBot.Enums;
using KBot.Helpers;

namespace KBot.Modules;

public class TourModule
{
    
    private readonly DiscordSocketClient _client;
    private readonly ConfigModel.Config _config;
    private static ulong TourRoleId;
    private static ulong TourAnnouncementChannelId;
    
    public TourModule(DiscordSocketClient client, ConfigModel.Config config)
    {
        _client = client;
        _config = config;
    }

    public void Initialize()
    {
        _client.GuildScheduledEventCreated += AnnounceScheduledEventCreated;
        _client.GuildScheduledEventUpdated += AnnounceScheduledEventUpdated;
        _client.GuildScheduledEventStarted += AnnounceScheduledEventStarted;
        _client.GuildScheduledEventCancelled += AnnounceScheduledEventCancelled;
        
        TourRoleId = _config.Tour.RoleId;
        TourAnnouncementChannelId = _config.Tour.EventAnnouncementChannelId;
    }
    
    private static async Task AnnounceScheduledEventCreated(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(TourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(TourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Scheduled));
        }
    }
    
    private static async Task AnnounceScheduledEventUpdated(Cacheable<SocketGuildEvent, ulong> arg1, SocketGuildEvent arg2)
    {
        var eventChannel = arg2.Channel;
        
        if (eventChannel is null && arg2.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg2.Guild.GetRole(TourRoleId);
            var notifyChannel = arg2.Guild.GetTextChannel(TourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, embed: await EmbedHelper.TourEventEmbed(arg2, EventEmbedType.Updated));
        }
    }
    
    private static async Task AnnounceScheduledEventStarted(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(TourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(TourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Started));
        }
    }
    
    private static async Task AnnounceScheduledEventCancelled(SocketGuildEvent arg)
    {
        var eventChannel = arg.Channel;
        
        if (eventChannel is null && arg.Location.Contains("goo.gl/maps"))
        {
            var tourRole = arg.Guild.GetRole(TourRoleId);
            var notifyChannel = arg.Guild.GetTextChannel(TourAnnouncementChannelId);
            await notifyChannel.SendMessageAsync(tourRole.Mention, embed: await EmbedHelper.TourEventEmbed(arg, EventEmbedType.Cancelled));
        }
    }
}