﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Zeenox.Extensions;

namespace Zeenox.Preconditions;

public sealed class RequireVoiceAttribute : PreconditionAttribute
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
            return Task.FromResult(
                PreconditionResult.FromError(cache.GetMessage(context.Guild.Id, "RequireVoiceChannel")));
        if (player is null)
            return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(userVoiceChannel.Id != player.VoiceChannelId
            ? PreconditionResult.FromError(cache.GetMessage(context.Guild.Id, "RequireSameVoiceChannel"))
            : PreconditionResult.FromSuccess());
    }
}