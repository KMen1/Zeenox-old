using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Autocompletes;

public class DjRoleAutocompleteHandler : AutocompleteHandler
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
        var roleIds = config.Music.DjRoleIds;

        var results = roleIds.Select(
            roleId =>
            {
                var role = context.Guild.GetRole(roleId);
                return role is null
                  ? new AutocompleteResult($"Deleted Role ({roleId})", roleId.ToString())
                  : new AutocompleteResult(role.Name, role.Id.ToString());
            }
        );

        return AutocompletionResult.FromSuccess(results.Take(5));
    }
}
