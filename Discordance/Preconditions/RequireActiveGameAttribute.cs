using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;
using Discordance.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public sealed class RequireActiveGameAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var gameService = services.GetRequiredService<GameService>();
        var cache = services.GetRequiredService<IMemoryCache>();

        var result = gameService.TryGetGame(context.User.Id, out var game);
        return result
            ? game?.UserId == context.User.Id
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(
                    PreconditionResult.FromError(cache.GetMessage(context.Guild.Id, "game_not_yours"))
                )
            : Task.FromResult(PreconditionResult.FromError(cache.GetMessage(context.Guild.Id, "user_not_playing")));
    }
}