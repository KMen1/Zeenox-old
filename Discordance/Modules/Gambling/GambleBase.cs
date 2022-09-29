using Discordance.Services;

namespace Discordance.Modules.Gambling;

public class GambleBase : ModuleBase
{
    public GameService GameService { get; set; } = null!;
}
