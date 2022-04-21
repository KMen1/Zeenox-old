using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Models;

namespace KBot.Modules.SelfRoles;

[Group("selfrole", "Self assignable roles using select menus")]
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

    [RequireUserPermission(GuildPermission.ManageRoles)]
    [SlashCommand("add", "Adds a role to the specified button role message.")]
    public async Task AddRoleToMessageAsync([Summary("messageid")] string messageIdString, string title, IRole role,
        string emote, string? description = null)
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

        var (result, reactionRoleMessage) = await Mongo.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.AddRole(new SelfRole(role.Id, title, emote, description))
        ).ConfigureAwait(false);
        if (!result)
        {
            await FollowupAsync("Could not find the specified message in the database.").ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage!.ChannelId)
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

        var (result, reactionRoleMessage) = await Mongo.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.RemoveRole(role)
        ).ConfigureAwait(false);
        if (!result)
        {
            await FollowupAsync("Could not find the specified message in the database.").ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage!.ChannelId)
            .GetMessageAsync(reactionRoleMessage.MessageId).ConfigureAwait(false) as IUserMessage;

        await dMessage!.ModifyAsync(x => x.Components = reactionRoleMessage.ToButtons()).ConfigureAwait(false);
        await FollowupAsync("Role removed!").ConfigureAwait(false);
    }

    [ComponentInteraction("roleselect", true)]
    public async Task HandleRoleButtonAsync(string[] selections)
    {
        await DeferAsync().ConfigureAwait(false);
        var msgId = ((SocketMessageComponent) Context.Interaction).Message.Id;
        var rr = await Mongo.GetSelfRoleMessageAsync(msgId).ConfigureAwait(false);
        var roleIds = rr.Roles.Select(x => x.RoleId).ToArray();
        var selectedIds = selections.Select(ulong.Parse).ToArray();
        var rolesToRemove = roleIds.Except(selectedIds).Select(x => Context.Guild.GetRole(x)).Where(x => x != null).ToArray();
        var rolesToAdd = selections.Select(x => Context.Guild.GetRole(ulong.Parse(x))).Where(x => x != null).ToArray();
        await ((SocketGuildUser)Context.User).AddRolesAsync(rolesToAdd).ConfigureAwait(false);
        await ((SocketGuildUser)Context.User).RemoveRolesAsync(rolesToRemove).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithDescription($"Successfully added {rolesToAdd.Length} roles and removed {rolesToRemove.Length} roles!")
            .WithColor(Color.Green)
            .Build();
        await FollowupAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}