using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Extensions;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace Discordance.Services;

public class TemporaryChannelService
{
    private readonly DiscordShardedClient _client;
    private readonly IConnectionMultiplexer _redis;
    private readonly MongoService _mongo;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<ulong, int> _channelCounts = new();

    public TemporaryChannelService(
        DiscordShardedClient client,
        MongoService mongo,
        IConnectionMultiplexer redis,
        IMemoryCache cache
    )
    {
        _client = client;
        _mongo = mongo;
        _redis = redis;
        _cache = cache;
        client.UserVoiceStateUpdated += CheckForCreationOrDeletionAsync;
    }

    private async Task CheckForCreationOrDeletionAsync(
        SocketUser socketUser,
        SocketVoiceState before,
        SocketVoiceState after
    )
    {
        if (socketUser is not SocketGuildUser user)
            return;

        if (user.IsBot)
            return;

        var guild = user.Guild;

        var hubs = _cache.GetGuildConfig(guild.Id).TcHubs;

        if (
            after.VoiceChannel is not null && hubs.Exists(x => x.ChannelId == after.VoiceChannel.Id)
        )
        {
            var hub = hubs.First(x => x.ChannelId == after.VoiceChannel.Id);
            if (_channelCounts.ContainsKey(guild.Id))
                _channelCounts[guild.Id]++;
            else
                _channelCounts.Add(guild.Id, 1);

            var voiceChannel = await guild
                .CreateVoiceChannelAsync(
                    ParseChannelName(hub.ChannelName, user, _channelCounts[guild.Id]),
                    x =>
                    {
                        x.UserLimit = hub.UserLimit;
                        x.CategoryId = hub.CategoryId;
                        x.Bitrate = hub.Bitrate;
                        x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(
                            new[]
                            {
                                new Overwrite(
                                    user.Id,
                                    PermissionTarget.User,
                                    new OverwritePermissions(connect: PermValue.Allow)
                                ),
                                new Overwrite(
                                    guild.EveryoneRole.Id,
                                    PermissionTarget.Role,
                                    new OverwritePermissions(connect: PermValue.Allow)
                                ),
                                new Overwrite(
                                    _client.CurrentUser.Id,
                                    PermissionTarget.User,
                                    new OverwritePermissions(
                                        viewChannel: PermValue.Allow,
                                        manageChannel: PermValue.Allow,
                                        connect: PermValue.Allow,
                                        moveMembers: PermValue.Allow
                                    )
                                )
                            }
                        );
                    }
                )
                .ConfigureAwait(false);
            await user.ModifyAsync(x => x.Channel = voiceChannel).ConfigureAwait(false);
            await _redis
                .GetDatabase()
                .StringSetAsync($"temp_channel_{user.Id.ToString()}", voiceChannel.Id)
                .ConfigureAwait(false);
        }

        if (
            before.VoiceChannel is not null
            && hubs.Exists(x => x.CategoryId == before.VoiceChannel.CategoryId)
            && before.VoiceChannel.ConnectedUsers.Count == 0
        )
        {
            await before.VoiceChannel.DeleteAsync().ConfigureAwait(false);
            _channelCounts[guild.Id]--;
            await _redis
                .GetDatabase()
                .KeyDeleteAsync($"temp_channel_{user.Id.ToString()}")
                .ConfigureAwait(false);
        }
    }

    public bool GetTempChannel(ulong userId, out ulong channelId)
    {
        var result = _redis.GetDatabase().StringGet($"temp_channel_{userId}");
        if (result.HasValue)
        {
            channelId = ulong.Parse(result.ToString());
            return true;
        }
        channelId = 0;
        return false;
    }

    private static string ParseChannelName(string channelName, IUser user, int index)
    {
        channelName = channelName.Replace(
            "{user.name}",
            $"{user.Username}",
            StringComparison.OrdinalIgnoreCase
        );
        channelName = channelName.Replace(
            "{index}",
            $"{index}",
            StringComparison.OrdinalIgnoreCase
        );
        return channelName;
    }
}
