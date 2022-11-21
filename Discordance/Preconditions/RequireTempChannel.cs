using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireTempChannel : PreconditionAttribute
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