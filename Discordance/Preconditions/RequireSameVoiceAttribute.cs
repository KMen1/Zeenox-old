using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireSameVoiceAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var service = services.GetRequiredService<IAudioService>();
        var player = service.GetPlayer(context.Guild.Id);

        return Task.FromResult(
            ((IVoiceState) context.User).VoiceChannel?.Id != player?.VoiceChannelId
                ? PreconditionResult.FromError("You must be in the same voice channel as the bot.")
                : PreconditionResult.FromSuccess()
        );
    }
}