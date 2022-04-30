using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowInteractions : SlashModuleBase
{
    private readonly HighLowService _highLowService;

    public HighLowInteractions(HighLowService highLowService)
    {
        _highLowService = highLowService;
    }

    [ComponentInteraction("highlow-high:*")]
    public async Task GuessHigherAsync(string id)
    {
        var game = _highLowService.GetGame(id);
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
        await game.GuessHigherAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-low:*")]
    public async Task GuessLowerAsync(string id)
    {
        var game = _highLowService.GetGame(id);
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
        await game.GuessLowerAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("highlow-finish:*")]
    public async Task FinishAsync(string id)
    {
        var game = _highLowService.GetGame(id);
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
        await game.FinishAsync().ConfigureAwait(false);
    }
}