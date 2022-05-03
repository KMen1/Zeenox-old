using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.SelfRoles;

[DefaultMemberPermissions(GuildPermission.ManageGuild)]
[Group("selfrole", "Self assignable roles using select menus")]
public class SelfRoleCommands : SlashModuleBase
{
    [SlashCommand("create", "Creates a new button role message")]
    public async Task CreateButtonRoleMessageAsync(string title, string description)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(Color.Blue)
            .Build();

        var msg = await Context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        await Mongo
            .AddSelfRoleMessageAsync(new SelfRoleMessage(
                Context.Guild.Id,
                Context.Channel.Id,
                msg.Id,
                title,
                description)).ConfigureAwait(false);
        var helpEmbed = new EmbedBuilder()
            .WithTitle("Message created!")
            .WithDescription(
                "To add roles use the **/br add** command, to remove a role you can use **/br remove** role!")
            .WithColor(Color.Green)
            .Build();
        await FollowupAsync(embed: helpEmbed).ConfigureAwait(false);
    }

    [SlashCommand("add", "Adds a role to the specified button role message.")]
    public async Task AddRoleToMessageAsync([Summary("messageid")] string messageIdString, string title, IRole role,
        string emote, string? description = null)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var parseResult = ulong.TryParse(messageIdString, NumberStyles.Any, CultureInfo.InvariantCulture,
            out var messageId);
        if (!parseResult)
        {
            var eb = new EmbedBuilder()
                .WithDescription("**Invalid message id**")
                .WithColor(Color.Red)
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
            return;
        }
        
        var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
        if (msg is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Could not find message with specified id in the current channel!.**")
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
            return;
        }

        var (result, reactionRoleMessage) = await Mongo.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.AddRole(new SelfRole(role.Id, title, emote, description))
        ).ConfigureAwait(false);
        if (!result)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Could not find message in the database!.**")
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage!.ChannelId)
            .GetMessageAsync(reactionRoleMessage.MessageId).ConfigureAwait(false) as IUserMessage;

        await dMessage!.ModifyAsync(x => x.Components = reactionRoleMessage.ToButtons()).ConfigureAwait(false);
        var seb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription($"**Succesfully added {role.Mention} to the message**")
            .Build();
        await FollowupAsync(embed: seb).ConfigureAwait(false);
    }

    [SlashCommand("remove", "Removes a role from the specified button role message.")]
    public async Task RemoveRoleFromMessageAsync([Summary("messageid")] string messageIdString, IRole role)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var parseResult = ulong.TryParse(messageIdString, NumberStyles.Any, CultureInfo.InvariantCulture,
            out var messageId);
        if (!parseResult)
        {
            var eb = new EmbedBuilder()
                .WithDescription("**Invalid message id**")
                .WithColor(Color.Red)
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
            return;
        }
        
        var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
        if (msg is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Could not find message with specified id in the current channel!.**")
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
            return;
        }

        var (result, reactionRoleMessage) = await Mongo.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.RemoveRole(role)
        ).ConfigureAwait(false);
        if (!result)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Could not find message in the database!.**")
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage!.ChannelId)
            .GetMessageAsync(reactionRoleMessage.MessageId).ConfigureAwait(false) as IUserMessage;

        await dMessage!.ModifyAsync(x => x.Components = reactionRoleMessage.ToButtons()).ConfigureAwait(false);
        var seb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription("**Succesfully removed role from the message!.**")
            .Build();
        await FollowupAsync(embed: seb).ConfigureAwait(false);
    }
}