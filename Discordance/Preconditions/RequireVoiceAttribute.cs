using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Discordance.Preconditions;

public class RequireVoiceAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        return ((IVoiceState) context.User).VoiceChannel is null
            ? Task.FromResult(PreconditionResult.FromError("You must be in a voice channel."))
            : Task.FromResult(PreconditionResult.FromSuccess());
    }
}