using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.Gambling.Crash;

public class CrashInteractions : SlashModuleBase
{
    private readonly CrashService _crashService;

    public CrashInteractions(CrashService crashService)
    {
        _crashService = crashService;
    }

    [ComponentInteraction("crash:*")]
    public async Task StopCrashGameAsync(string id)
    {
        var game = _crashService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await _crashService.StopGameAsync(id).ConfigureAwait(false);
    }
}
