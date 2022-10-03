using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireActiveGameAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var gameService = services.GetRequiredService<GameService>();

        var result = gameService.TryGetGame(context.User.Id, out var game);
        return result
            ? game?.UserId == context.User.Id
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(
                    PreconditionResult.FromError("You are not the player in this game.")
                )
            : Task.FromResult(PreconditionResult.FromError("You are currently not playing."));
    }
}