using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;
using Discordance.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public sealed class RequireSongRequesterAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var config = cache.GetGuildConfig(context.Guild.Id);

        if (!config.Music.ExclusiveControl)
            return Task.FromResult(PreconditionResult.FromSuccess());

        var service = services.GetRequiredService<AudioService>();
        var player = service.GetPlayer(context.Guild.Id);

        return context.User.Id == player!.RequestedBy.Id
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(
                PreconditionResult.FromError(cache.GetMessage(config.Language, "RequireSongRequester")));
    }
}