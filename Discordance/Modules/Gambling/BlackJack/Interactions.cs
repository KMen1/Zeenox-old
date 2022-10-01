using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;

namespace Discordance.Modules.Gambling.BlackJack;

public class Interactions : GambleBase
{
    [ComponentInteraction("blackjack-hit")]
    public async Task HitBlackJackAsync()
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

        var game = (BlackJackGame)generic!;
        var result = game.CanAffectGame(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game.HitAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("blackjack-stand")]
    public async Task StandBlackJackAsync()
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

        var game = (BlackJackGame)generic!;
        var result = game.CanAffectGame(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game.StandAsync().ConfigureAwait(false);
    }
}
