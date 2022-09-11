using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Discordance.Modules.SelfRoles;

public class Interactions : ModuleBase
{
    [ComponentInteraction("roleselect")]
    public async Task HandleRoleButtonAsync(string[] selections)
    {
        await DeferAsync().ConfigureAwait(false);
        var msgId = ((SocketMessageComponent)Context.Interaction).Message.Id;
        var config = await DatabaseService
            .GetGuildConfigAsync(Context.Guild.Id)
            .ConfigureAwait(false);
        var seflRoleMessage = config.SelfRoleMessages.First(x => x.MessageId == msgId);
        var roleIds = seflRoleMessage.Roles.Select(x => x.RoleId).ToArray();
        var selectedIds = selections.Select(ulong.Parse).ToArray();
        var rolesToRemove = roleIds
            .Except(selectedIds)
            .Select(x => Context.Guild.GetRole(x))
            .Where(x => x != null)
            .ToArray();
        var rolesToAdd = selections
            .Select(
                x =>
                    Context.Guild.GetRole(
                        ulong.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                    )
            )
            .Where(x => x != null)
            .ToArray();
        await ((SocketGuildUser)Context.User).AddRolesAsync(rolesToAdd).ConfigureAwait(false);
        await ((SocketGuildUser)Context.User).RemoveRolesAsync(rolesToRemove).ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithDescription(
                $"Successfully added {rolesToAdd.Length} roles and removed {rolesToRemove.Length} roles!"
            )
            .WithColor(Color.Green)
            .Build();
        await FollowupAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}
