using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackCommands : SlashModuleBase
{
    private readonly BlackJackService _blackJackService;

    public BlackJackCommands(BlackJackService blackJackService)
    {
        _blackJackService = blackJackService;
    }

    [SlashCommand("blackjack", "Starts a new game of Blackjack")]
    public async Task StartBlackJackAsync([MinValue(1)] [MaxValue(10000000)] int bet)
    {
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            await RespondAsync("Insufficient balance.").ConfigureAwait(false);
            return;
        }

        if (dbUser.MinimumBet > bet)
        {
            await RespondAsync($"You must bet at least {dbUser.MinimumBet} credits.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await RespondAsync("Starting Game...").ConfigureAwait(false);
        var msg = await GetOriginalResponseAsync().ConfigureAwait(true);
        var game = _blackJackService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}