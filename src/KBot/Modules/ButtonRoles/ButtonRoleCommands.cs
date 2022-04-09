using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.ButtonRoles;

[Group("br", "Gombal adható rangok")]
public class ButtonRoleCommands : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [SlashCommand("create", "Gomb rang üzenet létrehozása.")]
    public async Task CreateButtonRoleMessageAsync(string title, string description)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(Color.Blue)
            .Build();

        var msg = await Context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        await Database.AddReactionRoleMessageAsync(Context.Guild, new ButtonRoleMessage(Context.Channel.Id, msg.Id, title, description)).ConfigureAwait(false);
        await FollowupAsync("Létrehozva, rangot a /rr add paranccsal tudsz!").ConfigureAwait(false);
    }
    
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [SlashCommand("add", "Rang hozzáadása egy már meglévő üzenethez.")]
    public async Task AddRoleToMessageAsync([Summary("messageid")]string messageIdString, string title, IRole role, string emote)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var parseResult = ulong.TryParse(messageIdString, out var messageId);
        var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
        if (msg is null)
        {
            await FollowupAsync("Nem található a megadott üzenet ebben a csatornában.").ConfigureAwait(false);
            return;
        }

        var (result, reactionRoleMessage) = await Database.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.AddRole(new ButtonRole(role.Id, title, emote))
            ).ConfigureAwait(false);
        if (!result)
        {
            await FollowupAsync("Nem található a megadott üzenet az adatbázisban.").ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage.ChannelId)
            .GetMessageAsync(reactionRoleMessage.MessageId).ConfigureAwait(false) as IUserMessage;

        await dMessage!.ModifyAsync(x => x.Components = reactionRoleMessage.GetButtons()).ConfigureAwait(false);
        await FollowupAsync("Hozzáadva!").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageRoles)]
    [SlashCommand("remove", "Rang eltávolítása egy már meglévő üzenetből.")]
    public async Task RemoveRoleFromMessageAsync([Summary("messageid")]string messageIdString, IRole role)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var parseResult = ulong.TryParse(messageIdString, out var messageId);
        var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
        if (msg is null)
        {
            await FollowupAsync("Nem található a megadott üzenet ebben a csatornában.").ConfigureAwait(false);
            return;
        }

        var (result, reactionRoleMessage) = await Database.UpdateReactionRoleMessageAsync(
            Context.Guild,
            messageId,
            x => x.RemoveRole(role)
            ).ConfigureAwait(false);
        if (!result)
        {
            await FollowupAsync("Nem található a megadott üzenet az adatbázisban.").ConfigureAwait(false);
            return;
        }

        var dMessage = await Context.Guild.GetTextChannel(reactionRoleMessage.ChannelId)
            .GetMessageAsync(reactionRoleMessage.MessageId).ConfigureAwait(false) as IUserMessage;

        await dMessage!.ModifyAsync(x => x.Components = reactionRoleMessage.GetButtons()).ConfigureAwait(false);
        await FollowupAsync("Eltávolítva!").ConfigureAwait(false);
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
            await FollowupWithEmbedAsync(Color.Red, "Sikeresen levetted a rangot!", null, ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            await user.AddRoleAsync(role).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green, "Sikeresen felvetted a rangot!", null, ephemeral: true).ConfigureAwait(false);
        }
    }
}