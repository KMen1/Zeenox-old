using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Gambling.Mines;

public class MineComponents : SlashModuleBase
{
    public MinesService MinesService { get; set; }

    [ComponentInteraction("mine:*:*:*")]
    public async Task HandleMineAsync(string id, int x, int y)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = MinesService.GetGame(id);
        if (game.User.Id != Context.User.Id) return;
        await game.ClickFieldAsync(x, y).ConfigureAwait(false);
    }
}