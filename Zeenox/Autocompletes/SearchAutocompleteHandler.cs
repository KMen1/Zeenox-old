using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Zeenox.Extensions;
using Zeenox.Services;

namespace Zeenox.Autocompletes;

public sealed class SearchAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (autocompleteInteraction.Data.Current.Value is not string query || query.Length < 3)
            return AutocompletionResult.FromSuccess();

        var search = services.GetRequiredService<SearchService>();
        var results = await search.SearchAsync(query).ConfigureAwait(false);
        if (results.Length == 0)
            return AutocompletionResult.FromSuccess();

        var options = results.Select(x => new AutocompleteResult(x.Title.TrimTo(99), x.Url)).ToArray();
        return AutocompletionResult.FromSuccess(options);
    }
}