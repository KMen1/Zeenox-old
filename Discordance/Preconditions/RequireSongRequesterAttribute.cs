using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Modules.Music;
using Discordance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireSongRequesterAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var mongo = services.GetRequiredService<MongoService>();
        var config = await mongo.GetGuildConfigAsync(context.Guild.Id).ConfigureAwait(false);
        var musicConfig = config.Music;

        if (!musicConfig.ExclusiveControl)
            return PreconditionResult.FromSuccess();

        var service = services.GetRequiredService<AudioService>();
        var player = service.GetPlayer(context.Guild.Id);

        return context.User.Id == player.RequestedBy.Id
          ? PreconditionResult.FromSuccess()
          : PreconditionResult.FromError("You must be the song requester to perform this action.");
    }
}
