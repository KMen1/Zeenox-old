using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Extensions;

namespace Discordance.Modules.Gambling.HighLow;

public class Interactions : GambleBase
{
    [ComponentInteraction("highlow-high")]
    public async Task GuessHigherAsync()
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

        var game = (HighLowGame)generic;
        var result = game.CanAffectGame(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.HigherAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-low")]
    public async Task GuessLowerAsync()
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

        var game = (HighLowGame)generic;
        var result = game.CanAffectGame(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.LowerAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-finish")]
    public async Task FinishAsync()
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

        var game = (HighLowGame)generic;
        var result = game.CanAffectGame(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game!.FinishAsync().ConfigureAwait(false);
    }
}
