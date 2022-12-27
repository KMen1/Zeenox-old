using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Zeenox.Services;

namespace Zeenox.Preconditions;

public class RequireTempChannelAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
        ICommandInfo commandInfo, IServiceProvider services)
    {
        var service = services.GetRequiredService<TemporaryChannelService>();
        return Task.FromResult(service.HasTempChannel(context.User.Id)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be in a temporary channel to use this command."));
    }
}