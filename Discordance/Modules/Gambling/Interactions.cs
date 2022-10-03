using System.Threading.Tasks;
using Discord.Interactions;
using Discordance.Modules.Gambling.Games;
using Discordance.Preconditions;

namespace Discordance.Modules.Gambling;

[RequireActiveGame]
public class Interactions : GambleBase
{
    [ComponentInteraction("blackjack-hit")]
    public async Task HitBlackJackAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (BlackJack) GetGame();
        await game.HitAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("blackjack-stand")]
    public async Task StandBlackJackAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (BlackJack) GetGame();
        await game.StandAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-high")]
    public async Task GuessHigherAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (HighLow) GetGame();
        await game.HigherAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-low")]
    public async Task GuessLowerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (HighLow) GetGame();
        await game.LowerAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-finish")]
    public async Task FinishAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (HighLow) GetGame();
        await game.FinishAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("mine:*:*")]
    public async Task HandleMineAsync(int x, int y)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (Mines) GetGame();
        await game.ClickFieldAsync(x, y).ConfigureAwait(false);
    }

    [ComponentInteraction("towers:*:*")]
    public async Task ClickFieldAsync(int x, int y)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (Towers) GetGame();
        await game.ClickFieldAsync(x, y).ConfigureAwait(false);
    }

    [ComponentInteraction("crash")]
    public async Task StopCrashGameAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var game = (Crash) GetGame();
        await game.StopAsync().ConfigureAwait(false);
    }
}