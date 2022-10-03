using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequirePlayerAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var service = services.GetRequiredService<IAudioService>();

        return Task.FromResult(
            !service.HasPlayer(context.Guild.Id)
                ? PreconditionResult.FromError("I'm not connected!")
                : PreconditionResult.FromSuccess()
        );
    }
}