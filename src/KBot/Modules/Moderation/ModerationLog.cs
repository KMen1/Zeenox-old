using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Services;

namespace KBot.Modules.Moderation;

public class ModerationLog : IInjectable
{
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _database;

    public ModerationLog(DiscordSocketClient client, DatabaseService database)
    {
        _client = client;
        _database = database;
        
        _client.UserBanned += OnUserBanned;
        _client.UserUnbanned += OnUserUnbanned;
        
        _client.RoleCreated += OnRoleCreated;
        _client.RoleDeleted += OnRoleDeleted;
        
        _client.InviteCreated += OnInviteCreated;
        _client.InviteDeleted += OnInviteDeleted;
        
        _client.MessageDeleted += OnMessageDeleted;
        _client.MessageUpdated += OnMessageUpdated;
    }

    private async Task OnMessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        var beforeMessage = arg1.Value;
        if (beforeMessage.Author.IsBot || beforeMessage.Author.IsWebhook)
            return;
        var afterMessage = arg2;
        var channel = arg3 as SocketTextChannel;
        var guild = channel!.Guild;
        
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);
        
        var embed = new EmbedBuilder()
            .WithAuthor("Message edited", beforeMessage.Author.GetAvatarUrl())
            .AddField("User", beforeMessage.Author.Mention, true)
            .AddField("Channel", channel.Mention, true)
            .AddField("Before", $"```{beforeMessage.Content}```")
            .AddField("After", $"```{afterMessage.Content}```");
        
        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnMessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        var message = arg1.Value;
        if (message.Author.IsBot || message.Author.IsWebhook)
            return;
        var channel = arg2.Value as SocketTextChannel;
        var guild = channel!.Guild;

        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);

        var embed = new EmbedBuilder()
            .WithAuthor("Message Deleted", message.Author.GetAvatarUrl())
            .AddField("User", message.Author.Mention, true)
            .AddField("Channel", channel.Mention, true)
            .AddField("Content", $"```{message.Content}```");
        
        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private async Task OnUserBanned(SocketUser user, SocketGuild guild)
    {
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);
        
        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.Ban).FlattenAsync().ConfigureAwait(false);
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
    
    private async Task OnUserUnbanned(SocketUser user, SocketGuild guild)
    {
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);
        
        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.Unban).FlattenAsync().ConfigureAwait(false);
        var entry = auditLog.First();

        var embed = new EmbedBuilder()
            .WithAuthor("User Unbanned", user.GetAvatarUrl())
            .WithColor(Color.Green)
            .AddField("Unbanned by", entry.User.Mention)
            .AddField("User", user.Mention)
            .WithTimestamp(entry.CreatedAt);
        
        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
    
    private async Task OnRoleCreated(SocketRole role)
    {
        var guild = role.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);
        
        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.RoleCreated).FlattenAsync().ConfigureAwait(false);
        var entry = auditLog.First();
        
        var embed = new EmbedBuilder()
            .WithAuthor("Role Created", entry.User.GetAvatarUrl())
            .AddField("Created by", entry.User.Mention, true)
            .AddField("Role", role.Mention, true)
            .WithTimestamp(entry.CreatedAt)
            .WithColor(Color.Blue);
        
        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
    
    private async Task OnRoleDeleted(SocketRole role)
    {
        var guild = role.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);
        
        var auditLog = await guild.GetAuditLogsAsync(1, actionType: ActionType.RoleDeleted).FlattenAsync().ConfigureAwait(false);
        var entry = auditLog.First();
        
        var embed = new EmbedBuilder()
            .WithAuthor("Role Deleted", entry.User.GetAvatarUrl())
            .AddField("Deleted by", entry.User.Mention, true)
            .AddField("Role", role.Name, true)
            .WithTimestamp(entry.CreatedAt)
            .WithColor(Color.Red);
        
        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
    
    
    private async Task OnInviteCreated(SocketInvite invite)
    {
        var guild = invite.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);

        var embed = new EmbedBuilder()
            .WithAuthor("Invite Created", invite.Inviter.GetAvatarUrl())
            .AddField("Inviter", invite.Inviter.Mention, true)
            .AddField("Uses", $"`{invite.MaxUses}x`", true)
            .AddField("Age", invite.MaxAge, true)
            .AddField("Channel", invite.Channel.Name, true)
            .WithTimestamp(invite.CreatedAt)
            .WithColor(Color.Red);
        
        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
    
    private async Task OnInviteDeleted(SocketGuildChannel channel, string inviteCode)
    {
        var guild = channel.Guild;
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (!config.Moderation.Enabled)
            return;
        
        var logChannel = guild.GetTextChannel(config.Moderation.LogChannelId);
        
        var embed = new EmbedBuilder()
            .WithAuthor("Invite Deleted", guild.CurrentUser.GetAvatarUrl())
            .AddField("Code", inviteCode, true)
            .WithTimestamp(DateTime.UtcNow)
            .WithColor(Color.Red);
        
        await logChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
}