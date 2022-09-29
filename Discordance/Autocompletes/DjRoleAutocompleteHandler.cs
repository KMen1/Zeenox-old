using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;
using Discordance.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Discordance.Autocompletes;

public class DjRoleAutocompleteHandler : AutocompleteHandler
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

        return Task.FromResult(AutocompletionResult.FromSuccess(results.Take(5)));
    }
}
