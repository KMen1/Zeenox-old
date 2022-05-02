using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackInteractions : SlashModuleBase
{
    private readonly BlackJackService _blackJackService;

    public BlackJackInteractions(BlackJackService blackJackService)
    {
        _blackJackService = blackJackService;
    }

    [ComponentInteraction("blackjack-hit:*")]
    public async Task HitBlackJackAsync(string id)
    {
        var game = _blackJackService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }
        
        await DeferAsync().ConfigureAwait(false);
        await game!.HitAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("blackjack-stand:*")]
    public async Task StandBlackJackAsync(string id)
    {
        var game = _blackJackService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.StandAsync().ConfigureAwait(false);
    }
}