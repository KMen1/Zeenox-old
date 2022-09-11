using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireDjRoleAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var mongo = services.GetRequiredService<MongoService>();
        var config = await mongo.GetGuildConfigAsync(context.Guild.Id).ConfigureAwait(false);
        var musicConfig = config.Music;

        if (!musicConfig.DjOnly)
            return PreconditionResult.FromSuccess();

        return musicConfig.DjRoleIds
            .Intersect(((SocketGuildUser)context.User).Roles.Select(r => r.Id))
            .Any()
          ? PreconditionResult.FromSuccess()
          : PreconditionResult.FromError("This action requires a DJ role.");
    }
}
