using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Zeenox.Extensions;

namespace Zeenox.Preconditions;

public sealed class RequireAllowedChannelAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var config = cache.GetGuildConfig(context.Guild.Id);

        if (config.Music.AllowedVoiceChannels.Count == 0)
            return Task.FromResult(PreconditionResult.FromSuccess());

        return config.Music.AllowedVoiceChannels.Contains(((IVoiceState) context.User).VoiceChannel.Id)
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(
                PreconditionResult.FromError(cache.GetMessage(config.Language, "ChannelNotAllowed"))
            );
    }
}