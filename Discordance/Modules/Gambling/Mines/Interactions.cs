using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;

namespace Discordance.Modules.Gambling.Mines;

public class Interactions : GambleBase
{
    [ComponentInteraction("mine:*:*")]
    public async Task HandleMineAsync(int x, int y)
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

        var game = (MinesGame)generic;
        var result = game.CanAffectGame(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.ClickFieldAsync(x, y).ConfigureAwait(false);
    }
}
