using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;

namespace KBot.Modules;

public class TemporaryVoiceModule
{
    private readonly DiscordSocketClient _client;
    private readonly List<(SocketUser user, ulong channelId)> Channels = new();
    
    private readonly ulong CategoryId;
    private readonly ulong CreateChannelId;
    
    public TemporaryVoiceModule(DiscordSocketClient client, ConfigModel.Config config)
    {
        _client = client;
        CategoryId = config.TemporaryVoiceChannels.CategoryId;
        CreateChannelId = config.TemporaryVoiceChannels.CreateChannelId;
    }
    
    public Task InitializeAsync()
    {
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        return Task.CompletedTask;
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        var guild = after.VoiceChannel?.Guild ?? before.VoiceChannel?.Guild;
        if (guild == null)
        {
            return;
        }
        if (user.IsBot)
        {
            return;
        }
        
        if (after.VoiceChannel is not null && after.VoiceChannel.Id == CreateChannelId)
        {
            var userPermissions = new OverwritePermissions(manageChannel: PermValue.Allow, moveMembers: PermValue.Allow);
            var everyonePermissions = new OverwritePermissions(connect: PermValue.Deny);
            var botPermissions = new OverwritePermissions(viewChannel: PermValue.Allow, manageChannel: PermValue.Allow, connect: PermValue.Allow, moveMembers: PermValue.Allow);
            var voiceChannel = await guild.CreateVoiceChannelAsync($"{user.Username} Társalgója", x =>
            {
                x.UserLimit = 2;
                x.CategoryId = CategoryId;
                x.Bitrate = 96000;
                x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
                {
                    new Overwrite(user.Id, PermissionTarget.User,userPermissions),
                    new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, everyonePermissions),
                    new Overwrite(_client.CurrentUser.Id, PermissionTarget.Role, botPermissions)
                });
            });
            await guild.GetUser(user.Id).ModifyAsync(x => x.Channel = voiceChannel);
            Channels.Add((user, voiceChannel.Id));
        }
        else if (Channels.Contains((user, before.VoiceChannel?.Id ?? 0)))
        {
            var (puser, channelId) = Channels.First(x => x.user == user && x.channelId == before.VoiceChannel.Id);
            await guild.GetUser(puser.Id).ModifyAsync(x => x.Channel = null);
            await guild.GetChannel(channelId).DeleteAsync();
            Channels.Remove((puser, channelId));
        }
    }
}