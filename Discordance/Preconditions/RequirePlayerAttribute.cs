using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;
using Lavalink4NET;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public sealed class RequirePlayerAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var service = services.GetRequiredService<IAudioService>();
        var cache = services.GetRequiredService<IMemoryCache>();

        return Task.FromResult(
            !service.HasPlayer(context.Guild.Id)
                ? PreconditionResult.FromError(cache.GetMessage(context.Guild.Id, "no_player"))
                : PreconditionResult.FromSuccess()
        );
    }
}