using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Zeenox.Extensions;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Preconditions;

public sealed class RequireSongRequesterAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var config = cache.GetGuildConfig(context.Guild.Id);

        if (!config.Music.ExclusiveControl)
            return Task.FromResult(PreconditionResult.FromSuccess());

        var service = services.GetRequiredService<AudioService>();
        var player = service.GetPlayer(context.Guild.Id);

        var requester = (player?.CurrentTrack?.Context as TrackContext?)?.Requester;
        return context.User.Id == requester?.Id
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(
                PreconditionResult.FromError(cache.GetMessage(config.Language, "RequireSongRequester")));
    }
}