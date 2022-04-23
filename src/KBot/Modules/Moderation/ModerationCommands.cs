using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;
using KBot.Models;

namespace KBot.Modules.Moderation;

[Group("mod", "Moderation commands")]
public class ModerationCommands : SlashModuleBase
{
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("warn", "Warns a user.")]
    public async Task WarnAsync(SocketGuildUser user, string reason)
    {
        var moderatorId = Context.User.Id;
        await DeferAsync(true).ConfigureAwait(false);
        if (Context.Guild.GetUser(user.Id).GuildPermissions.KickMembers)
        {
            await FollowupWithEmbedAsync(Color.Red, "Unable to warn", "You can't warn another moderator")
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

    [RequireUserPermission(GuildPermission.KickMembers)]
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
            $"Succesfully deleted warn!", "").ConfigureAwait(false);
    }

    [SlashCommand("warns", "Gets the warns of a user.")]
    public async Task WarnsAsync(SocketUser user)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var warns = (await Mongo.GetWarnsAsync((SocketGuildUser)Context.User).ConfigureAwait(false)).ToList();
        if (warns.Count == 0)
        {
            await FollowupWithEmbedAsync(Color.Gold, "😎 Good job!",
                $"{user.Mention} has no warns!").ConfigureAwait(false);
            return;
        }

        var warnString = warns.Aggregate("", (current, warn) => current + $"`{warn.Id}`:`{warn.Date.ToString(CultureInfo.InvariantCulture)}` **By:** {Context.Client.GetUser(warn.GivenById).Mention} - **Reason:** `{warn.Reason}`\n");
        await FollowupWithEmbedAsync(Color.Orange, $"{user.Username} has {warns.Count} warns", warnString,
            ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("warninfo", "Get more information about a warn.")]
    public async Task WarnInfoAsync(string warnId)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var warn = await Mongo.GetWarnAsync(warnId).ConfigureAwait(false);
        if (warn == null)
        {
            await FollowupWithEmbedAsync(Color.Red, $"Warn with id({warnId}) does not exist", "").ConfigureAwait(false);
            return;
        }
        var moderator = Context.Client.GetUser(warn.GivenById);
        var user = Context.Client.GetUser(warn.GivenToId);
        var eb = new EmbedBuilder()
            .WithTitle($"Details of warn: {warn.Id}")
            .WithColor(Color.Orange)
            .AddField("Given By", $"{moderator.Mention}", true)
            .AddField("Given To", $"{user.Mention}", true)
            .AddField("Date", $"{warn.Date.ToString(CultureInfo.InvariantCulture)}", true)
            .AddField("Reason", $"```{warn.Reason}```")
            .WithCurrentTimestamp()
            .Build();
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageNicknames)]
    [SlashCommand("clearnicks", "Clears nicknames on the server.")]
    public async Task ClearNickNamesAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);

        var users = Context.Guild.Users.Where(x => x.Nickname != null).ToList();

        foreach (var user in users) await user.ModifyAsync(x => x.Nickname = null).ConfigureAwait(false);

        await FollowupWithEmbedAsync(Color.Green, "Success!", $"Cleared {users.Count} nicks.").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageMessages)]
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

    [SlashCommand("appeal", "Appeal a warn to the mod team.")]
    public async Task AppealAsync(
        [Summary("Admin", "The moderator who warned you")]
        SocketUser admin)
    {
        if (!Context.Guild.GetUser(admin.Id).GuildPermissions.KickMembers)
        {
            await RespondAsync("The selected user is not an admin.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Warn Appeal")
            .WithCustomId($"appeal:{admin.Id}")
            .AddTextInput("How were you punished?", "appeal-punishtype", TextInputStyle.Short,
                "Warn/mute/timeout", required: true)
            .AddTextInput("Why are you making an appeal?", "appeal-reason", TextInputStyle.Paragraph,
                "The admin warned me because he was angry, stb.", required: true)
            .AddTextInput("Why were you punished?", "appeal-punishreason", TextInputStyle.Paragraph,
                    "Rule breaking", required: true);
        await RespondWithModalAsync(modal.Build()).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setlog", "Sets the moderation log channel for the server.")]
    public async Task SetLogChannelAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.ModLogChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }    
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setappeal", "Sets the channel to send appeals to for the server.")]
    public async Task SetAppealChannelAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.AppealChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
}