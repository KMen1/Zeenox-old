using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;

namespace Discordance.Modules.Gambling.Crash;

public class Interactions : GambleBase
{
    [ComponentInteraction("crash")]
    public async Task StopCrashGameAsync()
    {
        if (!GameService.TryGetGame(Context.User.Id, out var generic))
        {
            await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("**You are currently not playing!**")
                        .Build()
                )
                .ConfigureAwait(false);
            return;
        }

        var game = (CrashGame)generic!;
        var result = game.CanAffectGame(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.StopAsync().ConfigureAwait(false);
    }
}
