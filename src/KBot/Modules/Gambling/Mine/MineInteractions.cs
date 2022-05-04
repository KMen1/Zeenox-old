using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.Gambling.Mine;

public class MineInteractions : SlashModuleBase
{
    private readonly MineService _minesService;

    public MineInteractions(MineService minesService)
    {
        _minesService = minesService;
    }

    [ComponentInteraction("mine:*:*:*")]
    public async Task HandleMineAsync(string id, int x, int y)
    {
        var game = _minesService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.ClickFieldAsync(x, y).ConfigureAwait(false);
    }
}
