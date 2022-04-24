using System.Threading.Tasks;
using Discord.Interactions;

namespace KBot.Modules.Gambling.Towers;

public class TowersComponents : SlashModuleBase
{
    private readonly TowersService _towersService;
    public TowersComponents(TowersService towersService)
    {
        _towersService = towersService;
    }

    [ComponentInteraction("towers:*:*:*")]
    public async Task ClickFieldAsync(string id, int x, int y)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = _towersService.GetGame(id);
        if (game?.User.Id != Context.User.Id)
            return;
        await game.ClickFieldAsync(x, y).ConfigureAwait(false);
    }
}