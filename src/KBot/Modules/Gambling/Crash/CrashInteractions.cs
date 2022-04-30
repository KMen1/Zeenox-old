using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

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
        if (game is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Game not found!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }
        
        if (game.User.Id != Context.User.Id)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**This is not your game!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await _crashService.StopGameAsync(id).ConfigureAwait(false);
    }
}