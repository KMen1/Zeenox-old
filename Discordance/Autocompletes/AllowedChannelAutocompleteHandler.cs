using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Extensions;
using Discordance.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Autocompletes;

public class AllowedChannelAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services
    )
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var config = cache.GetGuildConfig(context.Guild.Id);
        var channelIds = config.Music.AllowedVoiceChannels;

        var results = channelIds.Select(
            channelId =>
            {
                var channel = ((SocketGuild)context.Guild).GetVoiceChannel(channelId);
                return channel is null
                  ? new AutocompleteResult($"Deleted Channel ({channelId})", channelId.ToString())
                  : new AutocompleteResult(channel.Name, channel.Id.ToString());
            }
        );

        return Task.FromResult(AutocompletionResult.FromSuccess(results.Take(25)));
    }
}
