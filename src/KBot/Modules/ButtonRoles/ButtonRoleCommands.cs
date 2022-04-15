using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models.Guild;

namespace KBot.Modules.ButtonRoles;

[Group("br", "Self assignable roles using buttons")]
public class ButtonRoleCommands : SlashModuleBase
{
    [RequireUserPermission(GuildPermission.ManageRoles)]
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
        await Database
            .AddReactionRoleMessageAsync(Context.Guild,
                new ButtonRoleMessage(Context.Channel.Id, msg.Id, title, description)).ConfigureAwait(false);
        var helpEmbed = new EmbedBuilder()
            .WithTitle("Message created!")
            .WithDescription(
                "To add roles use the **/br add** command, to remove a role you can use **/br remove** role!")
            .WithColor(Color.Green);
        await FollowupAsync(embed: embed).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageRoles)]
    [SlashCommand("add", "Adds a role to the specified button role message.")]
    public async Task AddRoleToMessageAsync([Summary("messageid")] string messageIdString, string title, IRole role,
        string emote)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var parseResult = ulong.TryParse(messageIdString, out var messageId);
        var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
        if (msg is null)
        {
            await FollowupAsync($"Could not find message with id({messageIdString}) in the current channel!.")
                .ConfigureAwait(false);
            return;
        }

        var (result, reactionRoleMessage) = await Database.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.AddRole(new ButtonRole(role.Id, title, emote))
        ).ConfigureAwait(false);
        if (!result)
        {
            await FollowupAsync("Could not find the specified message in the database.").ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage.ChannelId)
            .GetMessageAsync(reactionRoleMessage.MessageId).ConfigureAwait(false) as IUserMessage;

        await dMessage!.ModifyAsync(x => x.Components = reactionRoleMessage.ToButtons()).ConfigureAwait(false);
        await FollowupAsync("Role added!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageRoles)]
    [SlashCommand("remove", "Removes a role from the specified button role message.")]
    public async Task RemoveRoleFromMessageAsync([Summary("messageid")] string messageIdString, IRole role)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var parseResult = ulong.TryParse(messageIdString, out var messageId);
        var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
        if (msg is null)
        {
            await FollowupAsync($"Could not find message with id({messageIdString}) in the current channel!.")
                .ConfigureAwait(false);
            return;
        }

        var (result, reactionRoleMessage) = await Database.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.RemoveRole(role)
        ).ConfigureAwait(false);
        if (!result)
        {
            await FollowupAsync("Could not find the specified message in the database.").ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage.ChannelId)
            .GetMessageAsync(reactionRoleMessage.MessageId).ConfigureAwait(false) as IUserMessage;

        await dMessage!.ModifyAsync(x => x.Components = reactionRoleMessage.ToButtons()).ConfigureAwait(false);
        await FollowupAsync("Role removed!").ConfigureAwait(false);
    }

    [ComponentInteraction("rrtr:*", true)]
    public async Task HandleRoleButtonAsync(ulong roleId)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var role = Context.Guild.GetRole(roleId);
        var user = Context.Guild.GetUser(Context.User.Id);
        if (user.Roles.Contains(role))
        {
            await user.RemoveRoleAsync(role).ConfigureAwait(false);
            var eb = new EmbedBuilder()
                .WithDescription($"**Succesfully removed {role.Mention} role**")
                .WithColor(Color.Red)
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
        }
        else
        {
            await user.AddRoleAsync(role).ConfigureAwait(false);
            var eb = new EmbedBuilder()
                .WithDescription($"**Succesfully added {role.Mention} role**")
                .WithColor(Color.Red)
                .Build();
            await FollowupAsync(embed: eb).ConfigureAwait(false);
        }
    }
}