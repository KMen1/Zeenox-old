using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;
using KBot.Models;

namespace KBot.Modules.Moderation;

public class AdminCommands : SlashModuleBase
{
    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    [SlashCommand("warn", "Warns a user.")]
    public async Task WarnAsync(SocketGuildUser user, string reason)
    {
        var moderatorId = Context.User.Id;
        await DeferAsync(true).ConfigureAwait(false);
        if (Context.Guild.GetUser(user.Id).GuildPermissions.KickMembers)
        {
            await FollowupWithEmbedAsync(Color.Red, "Unable to warn", "You can't warn another admin")
                .ConfigureAwait(false);
            return;
        }

        await Mongo.AddWarnAsync(new Warn(
            Guid.NewGuid().ToShortId(),
            Context.Guild.Id,
            moderatorId,
            user.Id,
            reason,
            DateTime.UtcNow), user).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Orange, $"Succesfully warned {user.Username}", "").ConfigureAwait(false);

        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle($"You've been warned in {Context.Guild.Name}!")
            .WithColor(Color.Red)
            .AddField("Moderator", $"{Context.User.Username}#{Context.User.DiscriminatorValue}")
            .AddField("Reason", $"```{reason}```")
            .WithCurrentTimestamp()
            .Build();
        try
        {
            await channel.SendMessageAsync(embed: eb).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
    }

    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    [SlashCommand("deletewarn", "Deletes a warn.")]
    public async Task RemoveWarnAsync(string warnId)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var result = await Mongo.RemoveWarnAsync(warnId).ConfigureAwait(false);
        if (!result)
        {
            await FollowupWithEmbedAsync(Color.Red, $"Warn with id({warnId}) does not exist", "").ConfigureAwait(false);
            return;
        }

        await FollowupWithEmbedAsync(Color.Green,
            "Succesfully deleted warn!", "").ConfigureAwait(false);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageNicknames)]
    [SlashCommand("clearnicks", "Clears nicknames on the server.")]
    public async Task ClearNickNamesAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);

        var users = Context.Guild.Users.Where(x => x.Nickname != null).ToList();

        foreach (var user in users) await user.ModifyAsync(x => x.Nickname = null).ConfigureAwait(false);

        await FollowupWithEmbedAsync(Color.Green, "Success!", $"Cleared {users.Count} nicks.").ConfigureAwait(false);
    }

    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    [SlashCommand("purge", "Deletes the specified amount of messages.")]
    public async Task PurgeAsync([MinValue(1)] [MaxValue(50)] int messages)
    {
        await DeferAsync(true).ConfigureAwait(false);

        var messagesToDelete =
            await Context.Channel.GetMessagesAsync(messages + 1).FlattenAsync().ConfigureAwait(false);
        messagesToDelete = messagesToDelete.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays < 14).ToList();

        await ((ITextChannel) Context.Channel).DeleteMessagesAsync(messagesToDelete).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Success!", $"Deleted {messagesToDelete.Count()} messages.")
            .ConfigureAwait(false);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("setmodlog", "Sets the moderation log channel for the server.")]
    public async Task SetLogChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.ModLogChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Moderation Log disabled!**"
                : $"**Moderation log will be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("setappeal", "Sets the channel to send appeals to for the server.")]
    public async Task SetAppealChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.AppealChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Appeals disabled**"
                : $"**Appeals will now be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
    
    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    [UserCommand("Warn User")]
    public async Task WarnUser(SocketGuildUser user)
    {
        if (user.GuildPermissions.KickMembers)
        {
            await RespondAsync(embed: new EmbedBuilder()
                    .WithDescription("**You can't warn another admin**")
                    .WithColor(Color.Red)
                    .Build())
                .ConfigureAwait(false);
            return;
        }
        
        var modal = new ModalBuilder()
            .WithTitle("Warn User")
            .AddTextInput("Reason", "reason-input", TextInputStyle.Paragraph)
            .WithCustomId($"warn-user:{user.Id}")
            .Build();
        await RespondWithModalAsync(modal).ConfigureAwait(false);
    }

    [ModalInteraction("warn-user:*")]
    public async Task HandleWarnAsync(ulong targetId, ReasonModal modal)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var guild = Context.Guild;
        var id = Guid.NewGuid().ToShortId();
        var targetUser = Context.Guild.GetUser(targetId);
        await Mongo.AddWarnAsync(new Warn(id, guild.Id, Context.User.Id, targetId, modal.Reason, DateTime.UtcNow), targetUser)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithDescription("**Successfully warned user**")
            .WithColor(Color.Green)
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}