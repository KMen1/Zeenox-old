using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.Gambling.Tower;

public class TowerInteractions : SlashModuleBase
{
    private readonly TowerService _towersService;

    public TowerInteractions(TowerService towersService)
    {
        _towersService = towersService;
    }

    [ComponentInteraction("towers:*:*:*")]
    public async Task ClickFieldAsync(string id, int x, int y)
    {
        var game = _towersService.GetGame(id);
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
