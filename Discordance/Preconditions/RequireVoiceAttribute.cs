using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;
using Lavalink4NET;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireVoiceAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var service = services.GetRequiredService<IAudioService>();
        var cache = services.GetRequiredService<IMemoryCache>();
        var player = service.GetPlayer(context.Guild.Id);

        var userVoiceChannel = ((IVoiceState) context.User).VoiceChannel;

        if (userVoiceChannel is null)
            return Task.FromResult(PreconditionResult.FromError(cache.GetMessage(context.Guild.Id, "require_voice_channel")));
        if (player is null)
            return Task.FromResult(PreconditionResult.FromSuccess());
        
        return Task.FromResult(userVoiceChannel.Id != player.VoiceChannelId
            ? PreconditionResult.FromError(cache.GetMessage(context.Guild.Id, "require_same_voice_channel")) 
            : PreconditionResult.FromSuccess());
    }
}