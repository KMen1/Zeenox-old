using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using Zeenox.Extensions;
using Zeenox.Models;

namespace Zeenox.Services;

public class TemporaryChannelService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<ulong, int> _channelCounts = new();
    private readonly ConcurrentDictionary<ulong, ulong> _channels = new();
    private readonly DiscordShardedClient _client;

    public TemporaryChannelService(
        DiscordShardedClient client,
        IMemoryCache cache
    )
    {
        _client = client;
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
        if (JoinedCreateChannel(after, out var hub))
        {
            if (HasTempChannel(user.Id))
                return;
            if (hub is null)
                return;

            var value = _channelCounts.AddOrUpdate(guild.Id, 0, (_, amount) => amount + 1);
            var voiceChannel = await guild
                .CreateVoiceChannelAsync(
                    ParseChannelName(hub.ChannelName, user, value),
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
                                    new OverwritePermissions(connect: PermValue.Allow, moveMembers: PermValue.Allow)
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
            _channels.TryAdd(user.Id, voiceChannel.Id);
        }

        if (LeftTempChannel(before, after, user, out var channel))
        {
            if (channel is not null)
                await channel.DeleteAsync().ConfigureAwait(false);
            _channelCounts.AddOrUpdate(guild.Id, 0, (_, amount) => amount - 1);
            _channels.TryRemove(user.Id, out _);
        }
    }

    public async Task<bool> LockChannelAsync(ulong userId)
    {
        if (!GetTempChannel(userId, out var channelId))
            return false;

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return false;

        await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole,
            new OverwritePermissions(connect: PermValue.Deny)).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> UnlockChannelAsync(ulong userId)
    {
        if (!GetTempChannel(userId, out var channelId))
            return false;

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return false;

        await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole,
            new OverwritePermissions(connect: PermValue.Allow)).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> LimitChannelAsync(ulong userId, int limit)
    {
        if (!GetTempChannel(userId, out var channelId))
            return false;

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return false;

        await channel.ModifyAsync(x => x.UserLimit = limit).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RenameChannelAsync(ulong userId, string name)
    {
        if (!GetTempChannel(userId, out var channelId))
            return false;

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return false;

        await channel.ModifyAsync(x => x.Name = name).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> KickUsersAsync(ulong userId, IEnumerable<ulong> users)
    {
        if (!GetTempChannel(userId, out var channelId))
            return false;

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return false;

        foreach (var user in users)
        {
            var member = await channel.Guild.GetUserAsync(user).ConfigureAwait(false);
            if (member is null)
                continue;
            await member.ModifyAsync(x => x.Channel = null).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<bool> BanUsersAsync(ulong userId, IEnumerable<ulong> users)
    {
        if (!GetTempChannel(userId, out var channelId))
            return false;

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return false;

        foreach (var user in users)
        {
            var member = await channel.Guild.GetUserAsync(user).ConfigureAwait(false);
            if (member is null)
                continue;
            await member.ModifyAsync(x => x.Channel = null).ConfigureAwait(false);
            await channel.AddPermissionOverwriteAsync(member,
                new OverwritePermissions(connect: PermValue.Deny)).ConfigureAwait(false);
        }

        return true;
    }

    public IEnumerable<IUser> GetBannedUsers(ulong userId)
    {
        if (!GetTempChannel(userId, out var channelId))
            return Enumerable.Empty<IUser>();

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return Enumerable.Empty<IUser>();

        return channel.PermissionOverwrites
            .Where(x => x.TargetType == PermissionTarget.User)
            .Where(x => x.Permissions.Connect == PermValue.Deny)
            .Select(x => _client.GetUser(x.TargetId));
    }

    public async Task<bool> UnbanUsersAsync(ulong userId, IEnumerable<ulong> users)
    {
        if (!GetTempChannel(userId, out var channelId))
            return false;

        if (_client.GetChannel(channelId) is not IVoiceChannel channel)
            return false;

        foreach (var user in users)
        {
            var member = await channel.Guild.GetUserAsync(user).ConfigureAwait(false);
            if (member is null)
                continue;
            await channel.AddPermissionOverwriteAsync(member,
                new OverwritePermissions(connect: PermValue.Allow)).ConfigureAwait(false);
        }

        return true;
    }

    private bool LeftTempChannel(SocketVoiceState before, SocketVoiceState after, IUser user,
        out IVoiceChannel? channel)
    {
        channel = null;
        if (before.VoiceChannel is not { } voiceChannel)
            return false;
        if (after.VoiceChannel is not null || after.VoiceChannel?.Id == voiceChannel.Id)
            return false;
        var categoryId = voiceChannel.CategoryId;
        channel = _client.GetChannel(voiceChannel.Id) as IVoiceChannel;
        return categoryId is not null && _channels.ContainsKey(user.Id);
    }

    private bool JoinedCreateChannel(SocketVoiceState after, out Hub? hub)
    {
        hub = null;
        if (after.VoiceChannel is not { } voiceChannel)
            return false;

        var categoryId = voiceChannel.CategoryId;
        if (categoryId is null)
            return false;

        var hubs = _cache.GetGuildConfig(voiceChannel.Guild.Id).Hubs;
        if (!hubs.Exists(x => x.CategoryId == categoryId))
            return false;

        hub = hubs.First(x => x.CategoryId == categoryId);

        return voiceChannel.Id == hub.ChannelId;
    }

    public bool HasTempChannel(ulong userId)
    {
        return _channels.ContainsKey(userId);
    }

    public bool GetTempChannel(ulong userId, out ulong channelId)
    {
        return _channels.TryGetValue(userId, out channelId);
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