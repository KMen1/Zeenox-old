using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireAllowedChannelAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var config = cache.GetGuildConfig(context.Guild.Id).Music;

        if (config.AllowedVoiceChannels.Count == 0)
            return Task.FromResult(PreconditionResult.FromSuccess());

        return config.AllowedVoiceChannels.Contains(((IVoiceState) context.User).VoiceChannel.Id)
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(
                PreconditionResult.FromError("Please use a channel that is on the allowlist.")
            );
    }
}