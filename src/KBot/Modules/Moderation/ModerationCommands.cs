using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Models;
using KBot.Models.Guild;
using KBot.Models.User;

namespace KBot.Modules.Moderation;

[Group("mod", "Moderation commands")]
public class ModerationCommands : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("warn", "Warns a user.")]
    public async Task WarnAsync(SocketUser user, string reason)
    {
        var moderatorId = Context.User.Id;
        await DeferAsync(true).ConfigureAwait(false);
        if (Context.Guild.GetUser(user.Id).GuildPermissions.KickMembers)
        {
            await FollowupWithEmbedAsync(Color.Red,"Unable to warn", "You can't warn another moderator").ConfigureAwait(false);
            return;
        }
        await Database.UpdateUserAsync(Context.Guild, user, x => x.Warns.Add(new Warn(moderatorId, reason, DateTime.UtcNow))).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Orange, $"Succesfully warned {user.Username}", "").ConfigureAwait(false);

        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle($"You've been warned in {Context.Guild.Name}!")
            .WithColor(Color.Red)
            .WithDescription($"By {Context.User.Mention}\nReason: `{reason}`")
            .WithCurrentTimestamp()
            .Build();
        try
        {
            await channel.SendMessageAsync(embed:eb).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("unwarn", "Unwarn a user.")]
    public async Task RemoveWarnAsync(SocketUser user, string reason, [MinValue(1)]int warnId)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        if (warnId > dbUser.Warns.Count)
        {
            await FollowupWithEmbedAsync(Color.Red,$"Warn with id({warnId}) does not exist", "").ConfigureAwait(false);
            return;
        }
        await Database.UpdateUserAsync(Context.Guild, user, x => x.Warns.RemoveAt(warnId - 1)).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green,
                $"Succesfully removed warn from {user.Username}!", "").ConfigureAwait(false);
        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle($"You've been unwarned in {Context.Guild.Name}!")
            .WithColor(Color.Green)
            .WithDescription($"By {Context.User.Mention}\nReason: `{reason}`")
            .WithCurrentTimestamp()
            .Build();
        try
        {
            await channel.SendMessageAsync(embed:eb).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
    }

    [SlashCommand("warns", "Gets the warns of a user.")]
    public async Task WarnsAsync(SocketUser user)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var warns = (await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false)).Warns;
        if (warns.Count == 0)
        {
            await FollowupWithEmbedAsync(Color.Gold, "😎 Nice job!",
                $"{user.Mention} has no warns!").ConfigureAwait(false);
            return;
        }

        var warnString = new StringBuilder();
        foreach (var warn in warns)
        {
            warnString.AppendLine(
                $"{warns.TakeWhile(n => n != warn).Count() + 1}. {Context.Client.GetUser(warn.ModeratorId).Mention} által - Indok:`{warn.Reason}`");
        }
        await FollowupWithEmbedAsync(Color.Orange, $"{user.Username} has {warns.Count} warns", warnString.ToString(), ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageNicknames)]
    [SlashCommand("clearnicks", "Clears nicknames on the server.")]
    public async Task ClearNickNamesAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);

        var users = Context.Guild.Users.Where(x => x.Nickname != null).ToList();

        foreach (var user in users)
        {
            await user.ModifyAsync(x => x.Nickname = null).ConfigureAwait(false);
        }

        await FollowupWithEmbedAsync(Color.Green, "Success!", $"Cleared {users.Count} nicks.").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageMessages)]
    [SlashCommand("purge", "Deletes the specified amount of messages.")]
    public async Task PurgeAsync([MinValue(1), MaxValue(50)]int messages)
    {
        await DeferAsync(true).ConfigureAwait(false);

        var messagesToDelete = await Context.Channel.GetMessagesAsync(messages + 1).FlattenAsync().ConfigureAwait(false);
        messagesToDelete = messagesToDelete.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays < 14).ToList();

        await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messagesToDelete).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Success!", $"Deleted {messagesToDelete.Count()} messages.").ConfigureAwait(false);
    }
    
    [SlashCommand("appeal", "Appeal a warn to the mod team.")]
    public async Task AppealAsync(
        [Summary("Admin", "The moderator who warned you")] SocketUser admin,
        [Summary("WarnID", "Warn id"), MinValue(1)] int warnId = 0)
    {
        if (!Context.Guild.GetUser(admin.Id).GuildPermissions.KickMembers)
        {
            await RespondAsync("The selected user is not an admin.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Warn Appeal")
            .AddTextInput("How were you punished?", "appeal-punishtype", TextInputStyle.Short,
                "Warn/mute/timeout", required: true)
            .AddTextInput("Why are you making an appeal?", "appeal-reason", TextInputStyle.Paragraph,
                "The admin warned me because he was angry, stb.", required: true);

        if (warnId == 0)
        {
            modal.WithCustomId($"appeal:{admin.Id}:0")
                .AddTextInput("Why were you punished?", "appeal-punishreason", TextInputStyle.Paragraph,
                    "Rule breaking", required: true);
            await RespondWithModalAsync(modal.Build()).ConfigureAwait(false);
            return;
        }
        var warns = (await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false)).Warns;
        if (warns.Count < warnId)
        {
            await RespondAsync("No warn exists with that id!", ephemeral: true).ConfigureAwait(false);
            return;
        }

        modal.WithCustomId($"appeal:{admin.Id}:{warnId}")
            .AddTextInput("Why were you punished?", "appeal-punishreason", TextInputStyle.Paragraph,
                "i sent kys in chat", required: false)
            .Build();

        await RespondWithModalAsync(modal.Build()).ConfigureAwait(false);
    }
}