using Discordance.Modules.Gambling.Games;
using Discordance.Services;

namespace Discordance.Modules.Gambling;

public class GambleBase : ModuleBase
{
    public GameService GameService { get; set; } = null!;

    protected IGame GetGame()
    {
        return GameService.GetGame(Context.User.Id);
    }
}