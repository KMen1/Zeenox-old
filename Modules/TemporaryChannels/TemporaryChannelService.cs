using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;

namespace KBot.Modules.TemporaryChannels;

public class TemporaryVoiceModule
{
    private readonly DiscordSocketClient _client;
    private readonly List<(SocketUser user, ulong channelId)> _channels = new();
    private readonly ulong _categoryId;
    private readonly ulong _createChannelId;
    public TemporaryVoiceModule(DiscordSocketClient client, ConfigModel.Config config)
    {
        _client = client;
        _categoryId = config.TemporaryVoiceChannels.CategoryId;
        _createChannelId = config.TemporaryVoiceChannels.CreateChannelId;
    }
    public void Initialize()
    {
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
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
        if (after.VoiceChannel is not null && after.VoiceChannel.Id == _createChannelId)
        {
            var voiceChannel = await guild.CreateVoiceChannelAsync($"{user.Username} Társalgója", x =>
            {
                x.UserLimit = 2;
                x.CategoryId = _categoryId;
                x.Bitrate = 96000;
                x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(new[]
                {
                    new Overwrite(user.Id, PermissionTarget.User,
                        new OverwritePermissions(manageChannel: PermValue.Allow, moveMembers: PermValue.Allow)),
                    new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role,
                        new OverwritePermissions(connect: PermValue.Deny)),
                    new Overwrite(_client.CurrentUser.Id, PermissionTarget.Role,
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
            await guild.GetUser(puser.Id).ModifyAsync(x => x.Channel = null).ConfigureAwait(false);
            await guild.GetChannel(channelId).DeleteAsync().ConfigureAwait(false);
            _channels.Remove((puser, channelId));
        }
    }
}