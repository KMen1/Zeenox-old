using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.SelfRoles;

public class SelfRoleInteractions : SlashModuleBase
{
    [ComponentInteraction("roleselect")]
    public async Task HandleRoleButtonAsync(string[] selections)
    {
        await DeferAsync().ConfigureAwait(false);
        var msgId = ((SocketMessageComponent) Context.Interaction).Message.Id;
        var rr = await Mongo.GetSelfRoleMessageAsync(msgId).ConfigureAwait(false);
        var roleIds = rr.Roles.Select(x => x.RoleId).ToArray();
        var selectedIds = selections.Select(ulong.Parse).ToArray();
        var rolesToRemove = roleIds.Except(selectedIds).Select(x => Context.Guild.GetRole(x)).Where(x => x != null)
            .ToArray();
        var rolesToAdd = selections
            .Select(x => Context.Guild.GetRole(ulong.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)))
            .Where(x => x != null).ToArray();
        await ((SocketGuildUser) Context.User).AddRolesAsync(rolesToAdd).ConfigureAwait(false);
        await ((SocketGuildUser) Context.User).RemoveRolesAsync(rolesToRemove).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithDescription($"Successfully added {rolesToAdd.Length} roles and removed {rolesToRemove.Length} roles!")
            .WithColor(Color.Green)
            .Build();
        await FollowupAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}