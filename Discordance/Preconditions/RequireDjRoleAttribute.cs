using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public sealed class RequireDjRoleAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var config = cache.GetGuildConfig(context.Guild.Id);

        if (!config.Music.DjOnly)
            return Task.FromResult(PreconditionResult.FromSuccess());

        return config.Music.DjRoleIds
            .Intersect(((SocketGuildUser) context.User).Roles.Select(r => r.Id))
            .Any()
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(
                PreconditionResult.FromError(cache.GetMessage(config.Language, "ActionRequiresDjRole")));
    }
}