using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackComponents : KBotModuleBase
{
    public BlackJackService BlackJackService { get; set; }
    
    [ComponentInteraction("blackjack-hit:*")]
    public async Task HitBlackJackAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = BlackJackService.GetGame(Id);
        if (game?.User.Id != Context.User.Id)
        {
            return;
        }
        await game.HitAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("blackjack-stand:*")]
    public async Task StandBlackJackAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = BlackJackService.GetGame(Id);
        if (game.User.Id != Context.User.Id)
        {
            return;
        }
        await game.StandAsync().ConfigureAwait(false);
    }
}