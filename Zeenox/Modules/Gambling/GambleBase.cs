using Zeenox.Modules.Gambling.Games;
using Zeenox.Services;

namespace Zeenox.Modules.Gambling;

public class GambleBase : ModuleBase
{
    public GameService GameService { get; set; } = null!;

    protected IGame GetGame()
    {
        return GameService.GetGame(Context.User.Id);
    }
}