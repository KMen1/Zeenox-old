using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Database;

namespace KBot.Modules.TemporaryChannels;

public class TemporaryVoiceModule
{
    private readonly DiscordSocketClient _client;
    private static DatabaseService _database;
    private readonly List<(SocketUser user, ulong channelId)> _channels = new();
    public TemporaryVoiceModule(DiscordSocketClient client, DatabaseService database)
    {
        _client = client;
        _database = database;
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
        var config = await _database.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
        if (after.VoiceChannel is not null && after.VoiceChannel.Id == config.TemporaryChannels.CreateChannelId)
        {
            var voiceChannel = await guild.CreateVoiceChannelAsync($"{user.Username} Társalgója", x =>
            {
                x.UserLimit = 2;
                x.CategoryId = config.TemporaryChannels.CategoryId;
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