using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowInteractions : SlashModuleBase
{
    private readonly HighLowService _highLowService;

    public HighLowInteractions(HighLowService highLowService)
    {
        _highLowService = highLowService;
    }

    [ComponentInteraction("highlow-high:*")]
    public async Task GuessHigherAsync(string id)
    {
        var game = _highLowService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.GuessHigherAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-low:*")]
    public async Task GuessLowerAsync(string id)
    {
        var game = _highLowService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.GuessLowerAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-finish:*")]
    public async Task FinishAsync(string id)
    {
        var game = _highLowService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.FinishAsync().ConfigureAwait(false);
    }
}
