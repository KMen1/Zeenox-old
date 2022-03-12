using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Gambling.Mines;

public class MineComponents : KBotModuleBase
{
    [ComponentInteraction("mine:*:*:*")]
    public async Task HandleMineAsync(string id, string x, string y)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetMinesGame(id);
        if (game.User.Id != Context.User.Id)
        {
            return;
        }
        await game.ClickFieldAsync(int.Parse(x), int.Parse(y)).ConfigureAwait(false);
    }
}