using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackInteractions : SlashModuleBase
{
    private readonly BlackJackService _blackJackService;

    public BlackJackInteractions(BlackJackService blackJackService)
    {
        _blackJackService = blackJackService;
    }

    [ComponentInteraction("blackjack-hit:*")]
    public async Task HitBlackJackAsync(string id)
    {
        var game = _blackJackService.GetGame(id);
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
        await game.HitAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("blackjack-stand:*")]
    public async Task StandBlackJackAsync(string id)
    {
        var game = _blackJackService.GetGame(id);
        if (game is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Game not found!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        if (game.User.Id != Context.User.Id) return;
        await game.StandAsync().ConfigureAwait(false);
    }
}