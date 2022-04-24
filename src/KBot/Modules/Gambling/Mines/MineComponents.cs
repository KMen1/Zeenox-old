using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Gambling.Mines;

public class MineComponents : SlashModuleBase
{
    private readonly MinesService _minesService;
    public MineComponents(MinesService minesService)
    {
        _minesService = minesService;
    }
    
    [ComponentInteraction("mine:*:*:*")]
    public async Task HandleMineAsync(string id, int x, int y)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = _minesService.GetGame(id);
        if (game?.User.Id != Context.User.Id) return;
        await game.ClickFieldAsync(x, y).ConfigureAwait(false);
    }
}