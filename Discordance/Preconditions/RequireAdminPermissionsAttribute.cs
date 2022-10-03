using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Discordance.Preconditions;

public class RequireAdminPermissionsAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var user = (SocketGuildUser) context.User;
        if (user.Roles.ToList().Exists(x => x.Permissions.Administrator))
            return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(
            PreconditionResult.FromError(
                "You must have Administrator permissions to use this command."
            )
        );
    }
}