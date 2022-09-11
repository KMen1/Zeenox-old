using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Autocompletes;

public class SelfRoleMessageAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services
    )
    {
        var mongo = services.GetRequiredService<MongoService>();
        var config = await mongo.GetGuildConfigAsync(context.Guild.Id).ConfigureAwait(false);

        var results = config.SelfRoleMessages.Select(
            message =>
                new AutocompleteResult(message.Title, $"{message.ChannelId}:{message.MessageId}")
        );

        return AutocompletionResult.FromSuccess(results);
    }
}
