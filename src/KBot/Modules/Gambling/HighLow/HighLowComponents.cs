using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowComponents : KBotModuleBase
{
    [ComponentInteraction("highlow-high:*")]
    public async Task HighLowHigh(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetHighLowGame(Id);
        if (game?.User.Id != Context.User.Id)
        {
            return;
        }
        await game.GuessHigherAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("highlow-low:*")]
    public async Task HighLowLow(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetHighLowGame(Id);
        if (game?.User.Id != Context.User.Id)
        {
            return;
        }

        await game.GuessLowerAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("highlow-finish:*")]
    public async Task HighLowFinish(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetHighLowGame(Id);
        if (game?.User.Id != Context.User.Id)
        {
            return;
        }

        await game.FinishAsync().ConfigureAwait(false);
    }
}