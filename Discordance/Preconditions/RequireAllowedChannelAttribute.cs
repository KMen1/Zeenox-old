using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Preconditions;

public class RequireAllowedChannelAttribute : PreconditionAttribute
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

        if (musicConfig.AllowedVoiceChannels.Count == 0)
            return PreconditionResult.FromSuccess();

        return musicConfig.AllowedVoiceChannels.Contains(
            ((IVoiceState)context.User).VoiceChannel.Id
        )
          ? PreconditionResult.FromSuccess()
          : PreconditionResult.FromError("Please use a channel that is on the allowlist.");
    }
}
