using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using KBot.Services;
using Serilog;

namespace KBot.Modules.Guild;

public class GuildEvents : IInjectable
{
    private readonly List<(SocketGuildUser user, ulong channelId)> _channels;
    private readonly DiscordSocketClient _client;
    private readonly MongoService _mongo;

    public GuildEvents(DiscordSocketClient client, MongoService database)
    {
        _client = client;
        _mongo = database;
        client.GuildAvailable += ClientOnGuildAvailableAsync;
        client.UserJoined += AnnounceUserJoinedAsync;
        client.UserLeft += AnnounceUserLeftAsync;
        client.UserBanned += AnnounceUserBannedAsync;
        client.UserUnbanned += AnnounceUserUnbannedAsync;
        client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        client.GuildMemberUpdated += OnGuildMemberUpdatedAsync;
        _channels = new List<(SocketGuildUser user, ulong channelId)>();
        Task.Run(DeleteChannelsAsync);
    }

    private async Task OnGuildMemberUpdatedAsync(
        Cacheable<SocketGuildUser, ulong> before,
        SocketGuildUser after
    )
    {
        var beforeUser = await before.GetOrDownloadAsync().ConfigureAwait(false);
        if (beforeUser is null)
            return;

        var rolesChanged = beforeUser.Roles
            .OrderBy(x => x)
            .SequenceEqual(after.Roles.OrderBy(x => x));
        if (!rolesChanged)
            return;

        await _mongo
            .UpdateUserAsync(
                after,
                x => x.Roles = after.Roles.Where(y => !y.IsEveryone).Select(z => z.Id).ToList()
            )
            .ConfigureAwait(false);
    }

    private async Task DeleteChannelsAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            try
            {
                foreach (var (user, channelId) in _channels.ToList())
                {
                    var channel = user.Guild.GetVoiceChannel(channelId);
                    if (channel is null)
                    {
                        _channels.Remove((user, channelId));
                        continue;
                    }

                    if (channel.Users.Count > 0)
                        continue;
                    await channel.DeleteAsync().ConfigureAwait(false);
                    _channels.Remove((user, channelId));
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Error in temporary channel deletion");
            }
        }
    }

    private async Task OnUserVoiceStateUpdatedAsync(
        SocketUser argUser,
        SocketVoiceState before,
        SocketVoiceState after
    )
    {
        var user = (SocketGuildUser)argUser;
        var guild = user.Guild;
        if (user.IsBot)
            return;
        var config = await _mongo.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.TemporaryVoiceCreateId == 0)
            return;

        if (
            after.VoiceChannel is not null && after.VoiceChannel.Id == config.TemporaryVoiceCreateId
        )
        {
            var voiceChannel = await guild
                .CreateVoiceChannelAsync(
                    $"{user.Username}'s Voice",
                    x =>
                    {
                        x.UserLimit = 2;
                        x.CategoryId = config.TemporaryVoiceCategoryId;
                        x.Bitrate = 96000;
                        x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(
                            new[]
                            {
                                new Overwrite(
                                    user.Id,
                                    PermissionTarget.User,
                                    new OverwritePermissions(
                                        connect: PermValue.Allow,
                                        moveMembers: PermValue.Allow,
                                        manageChannel: PermValue.Allow
                                    )
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
            _channels.Add((user, voiceChannel.Id));
        }
    }

    private async Task ClientOnGuildAvailableAsync(SocketGuild guild)
    {
        if (!await _mongo.IsGuildRegisteredAsync(guild).ConfigureAwait(false))
            await _mongo.CreateGuildConfigAsync(guild).ConfigureAwait(false);
    }

    private async Task AnnounceUserJoinedAsync(SocketGuildUser user)
    {
        if (user.IsBot || user.IsWebhook)
            return;
        var dbUser = await _mongo.GetUserAsync(user).ConfigureAwait(false);
        var config = await _mongo.GetGuildConfigAsync(user.Guild).ConfigureAwait(false);
        foreach (var roleId in dbUser.Roles)
        {
            var guild = user.Guild;
            var role = guild.GetRole(roleId);
            if (role is null)
                continue;
            await user.AddRoleAsync(role).ConfigureAwait(false);
        }

        await user.AddRoleAsync(config.WelcomeRoleId).ConfigureAwait(false);

        if (config.WelcomeChannelId == 0)
            return;
        var channel = user.Guild.GetTextChannel(config.WelcomeChannelId);
        var eb = new EmbedBuilder()
            .WithAuthor($"{user.Username}#{user.Discriminator}", user.GetAvatarUrl())
            .WithColor(Color.Green)
            .WithDescription(
                $"Welcome to **{user.Guild.Name}** {user.Mention}!\n"
                    + $"You are the **{user.Guild.Users.Count.Ordinalize(CultureInfo.InvariantCulture)}** member!\n"
                    + $"Account created: **<t:{user.CreatedAt.ToUnixTimeSeconds()}:R>**"
            )
            .WithCurrentTimestamp()
            .WithThumbnailUrl(user.GetAvatarUrl())
            .Build();
        await channel.SendMessageAsync(embed: eb).ConfigureAwait(false);
    }

    private async Task AnnounceUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        if (user.IsBot || user.IsWebhook)
            return;
        var config = await _mongo.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.LeaveChannelId == 0)
            return;
        if (await guild.GetBanAsync(user).ConfigureAwait(false) is not null)
            return;
        var channel = guild.GetTextChannel(config.LeaveChannelId);
        var eb = new EmbedBuilder()
            .WithAuthor($"{user.Username}#{user.Discriminator}", user.GetAvatarUrl())
            .WithColor(Color.Red)
            .WithDescription(
                $"{user.Mention} left the server!\n"
                    + $"Account created: **<t:{user.CreatedAt.ToUnixTimeSeconds()}:R>**"
            )
            .WithCurrentTimestamp()
            .WithThumbnailUrl(user.GetAvatarUrl())
            .Build();
        await channel.SendMessageAsync(embed: eb).ConfigureAwait(false);
    }

    private async Task AnnounceUserBannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
            return;
        var config = await _mongo.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.BanChannelId == 0)
            return;

        var log = await guild
            .GetAuditLogsAsync(1, actionType: ActionType.Ban)
            .FlattenAsync()
            .ConfigureAwait(false);
        var entry = log.First();
        var admin = entry.User;
        var reason = entry.Reason;

        var channel = guild.GetTextChannel(config.BanChannelId);
        var eb = new EmbedBuilder()
            .WithAuthor($"{user.Username}#{user.Discriminator}", user.GetAvatarUrl())
            .WithColor(Color.Red)
            .WithDescription(
                $"**{user.Mention}** has been banned!\n"
                    + $"Account created: **<t:{user.CreatedAt.ToUnixTimeSeconds()}:R>**"
            )
            .AddField("Banned by", $"{admin.Mention}", true)
            .AddField("Reason", reason is not null ? $"`{reason}`" : "`No reason given.`", true)
            .WithCurrentTimestamp()
            .WithThumbnailUrl(user.GetAvatarUrl())
            .Build();
        await channel.SendMessageAsync(embed: eb).ConfigureAwait(false);
    }

    private async Task AnnounceUserUnbannedAsync(SocketUser user, SocketGuild guild)
    {
        if (user.IsBot || user.IsWebhook)
            return;
        var config = await _mongo.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.UnbanChannelId == 0)
            return;
        var channel = guild.GetTextChannel(config.UnbanChannelId);
        var eb = new EmbedBuilder()
            .WithAuthor($"{user.Username}#{user.Discriminator}", user.GetAvatarUrl())
            .WithColor(Color.Green)
            .WithDescription(
                $"{user.Mention} has been unbanned!\n"
                    + $"Account created: **<t:{user.CreatedAt.ToUnixTimeSeconds()}:R>**"
            )
            .WithCurrentTimestamp()
            .WithThumbnailUrl(user.GetAvatarUrl())
            .Build();
        await channel.SendMessageAsync(embed: eb).ConfigureAwait(false);
    }
}
