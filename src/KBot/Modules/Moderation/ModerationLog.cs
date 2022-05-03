using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using KBot.Services;

namespace KBot.Modules.Moderation;

public class ModerationLog : IInjectable
{
    private readonly DiscordSocketClient _client;
    private readonly MongoService _database;

    private readonly Regex _inviteRegex = new(
        @"(https?://)?(www.)?(discord.(gg|io|me|li)|discordapp.com/invite)/[^\s/]+?(?=\b)"
        , RegexOptions.Compiled);

    public ModerationLog(DiscordSocketClient client, MongoService database)
    {
        _client = client;
        _database = database;

        _client.UserBanned += OnUserBannedAsync;
        _client.UserUnbanned += OnUserUnbannedAsync;

        _client.RoleCreated += OnRoleCreatedAsync;
        _client.RoleDeleted += OnRoleDeletedAsync;

        _client.InviteCreated += OnInviteCreatedAsync;
        _client.InviteDeleted += OnInviteDeletedAsync;

        _client.MessageDeleted += OnMessageDeletedAsync;
        _client.MessageUpdated += OnMessageUpdatedAsync;
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    private async Task OnMessageReceivedAsync(SocketMessage arg)
    {
        if (arg.Author.IsBot) return;
        var channel = (SocketTextChannel) arg.Channel;
        var guild = channel.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;
        if (!_inviteRegex.IsMatch(arg.Content)) return;
        var invites = _inviteRegex.Matches(arg.Content);

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        foreach (var invite in invites.Select(match => match.Value))
        {
            if ((await _client.GetInviteAsync(invite).ConfigureAwait(false)).GuildId == guild.Id) continue;
            var embed = new EmbedBuilder()
                .WithAuthor("Invite Sent", arg.Author.GetAvatarUrl())
                .AddField("User", arg.Author.Mention, true)
                .AddField("Channel", channel.Mention, true)
                .AddField("Invite", invite)
                .Build();
            await logChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        await arg.DeleteAsync().ConfigureAwait(false);
    }

    private async Task OnMessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage afterMessage,
        ISocketMessageChannel iChannel)
    {
        var beforeMessage = await before.GetOrDownloadAsync().ConfigureAwait(false);
        if (beforeMessage is null) return;
        if (beforeMessage.Author.IsBot || beforeMessage.Author.IsWebhook)
            return;
        if (beforeMessage.Content.Equals(afterMessage.Content, StringComparison.OrdinalIgnoreCase)) return;
        var channel = (SocketTextChannel)iChannel;
        var guild = channel.Guild;

        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var embed = new EmbedBuilder()
            .WithAuthor("Message edited", beforeMessage.Author.GetAvatarUrl())
            .AddField("User", beforeMessage.Author.Mention, true)
            .AddField("Channel", channel.Mention, true)
            .AddField("Before", $"```{beforeMessage.Content}```")
            .AddField("After", $"```{afterMessage.Content}```");

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        var message = await arg1.GetOrDownloadAsync().ConfigureAwait(false);
        var channel = await arg2.GetOrDownloadAsync().ConfigureAwait(false);
        if (message is null || channel is null) return;
        if (message.Author.IsBot || message.Author.IsWebhook)
            return;
        
        var guild = ((SocketTextChannel)channel).Guild;

        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var embed = new EmbedBuilder()
            .WithAuthor("Message Deleted", message.Author.GetAvatarUrl())
            .AddField("User", message.Author.Mention, true)
            .AddField("Channel", ((SocketTextChannel)channel).Mention, true)
            .AddField("Content", $"```{message.Content}```");

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnUserBannedAsync(SocketUser user, SocketGuild guild)
    {
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.Ban).FlattenAsync()
            .ConfigureAwait(false);
        var entry = auditLog.First();

        var embed = new EmbedBuilder()
            .WithAuthor("User Banned", user.GetAvatarUrl())
            .WithColor(Color.Red)
            .AddField("Banned by", entry.User.Mention, true)
            .AddField("User", user.Mention, true)
            .AddField("Reason", $"```{entry.Reason}```")
            .WithTimestamp(entry.CreatedAt);

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnUserUnbannedAsync(SocketUser user, SocketGuild guild)
    {
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.Unban).FlattenAsync()
            .ConfigureAwait(false);
        var entry = auditLog.First();
        
        var embed = new EmbedBuilder()
            .WithAuthor("User Unbanned", user.GetAvatarUrl())
            .WithColor(Color.Green)
            .AddField("Unbanned by", entry.User.Mention)
            .AddField("User", user.Mention)
            .WithTimestamp(entry.CreatedAt);

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnRoleCreatedAsync(SocketRole role)
    {
        var guild = role.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.RoleCreated).FlattenAsync()
            .ConfigureAwait(false);
        var entry = auditLog.First();
        var user = entry.User;
        if (entry.CreatedAt < DateTimeOffset.UtcNow.AddMinutes(-1))
            user = null;

        var embed = new EmbedBuilder()
            .WithAuthor("Role Created", user is null ? guild.IconUrl : user.GetAvatarUrl())
            .AddField("Created by", user is null ? "Unknown" : user.Mention, true)
            .AddField("Role", role.Mention, true)
            .WithTimestamp(entry.CreatedAt)
            .WithColor(Color.Blue);

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnRoleDeletedAsync(SocketRole role)
    {
        var guild = role.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.RoleDeleted).FlattenAsync()
            .ConfigureAwait(false);
        var entry = auditLog.First();
        var user = entry.User;
        if (entry.CreatedAt < DateTimeOffset.UtcNow.AddMinutes(-1))
            user = null;

        var embed = new EmbedBuilder()
            .WithAuthor("Role Deleted", user is null ? guild.IconUrl : user.GetAvatarUrl())
            .AddField("Deleted by", user is null ? "Unknown" : user.Mention, true)
            .AddField("Role", role.Name, true)
            .WithTimestamp(entry.CreatedAt)
            .WithColor(Color.Red);

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }


    private async Task OnInviteCreatedAsync(SocketInvite invite)
    {
        var guild = invite.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var embed = new EmbedBuilder()
            .WithAuthor("Invite Created", invite.Inviter.GetAvatarUrl())
            .AddField("Inviter", invite.Inviter.Mention, true)
            .AddField("Uses", $"`{invite.MaxUses}x`", true)
            .AddField("Age", TimeSpan.FromSeconds(invite.MaxAge).Humanize(), true)
            .AddField("Channel", invite.Channel.Name, true)
            .WithTimestamp(invite.CreatedAt)
            .WithColor(Color.Green);

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnInviteDeletedAsync(SocketGuildChannel channel, string inviteCode)
    {
        var guild = channel.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config.ModLogChannelId == 0)
            return;

        var logChannel = guild.GetTextChannel(config.ModLogChannelId);

        var embed = new EmbedBuilder()
            .WithAuthor("Invite Deleted", guild.CurrentUser.GetAvatarUrl())
            .AddField("Code", inviteCode, true)
            .WithTimestamp(DateTime.UtcNow)
            .WithColor(Color.Red);

        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
}