using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowComponents : KBotModuleBase
{
    public HighLowService HighLowService { get; set; }
    
    [ComponentInteraction("highlow-high:*")]
    public async Task GuessHigherAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = HighLowService.GetGame(Id);
        if (game?.User.Id != Context.User.Id)
        {
            return;
        }
        await game.GuessHigherAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("highlow-low:*")]
    public async Task GuessLowerAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = HighLowService.GetGame(Id);
        if (game?.User.Id != Context.User.Id)
        {
            return;
        }

        await game.GuessLowerAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("highlow-finish:*")]
    public async Task FinishAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = HighLowService.GetGame(Id);
        if (game?.User.Id != Context.User.Id)
        {
            return;
        }

        await game.FinishAsync().ConfigureAwait(false);
    }
}