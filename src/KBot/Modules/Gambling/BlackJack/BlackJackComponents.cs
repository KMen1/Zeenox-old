using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackComponents : SlashModuleBase
{
    private readonly BlackJackService _blackJackService;

    public BlackJackComponents(BlackJackService blackJackService)
    {
        _blackJackService = blackJackService;
    }

    [ComponentInteraction("blackjack-hit:*")]
    public async Task HitBlackJackAsync(string Id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = _blackJackService.GetGame(Id);
        if (game?.User.Id != Context.User.Id) return;
        await game.HitAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("blackjack-stand:*")]
    public async Task StandBlackJackAsync(string Id)
    {
        var game = _blackJackService.GetGame(Id);
        
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