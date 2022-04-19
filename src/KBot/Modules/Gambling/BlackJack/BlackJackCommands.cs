using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackCommands : SlashModuleBase
{
    public BlackJackService BlackJackService { get; set; }

    [SlashCommand("blackjack", "Starts a new game of Blackjack")]
    public async Task StartBlackJackAsync([MinValue(100)] [MaxValue(1000000)] int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            await FollowupAsync("Insufficient balance.").ConfigureAwait(false);
            return;
        }

        var msg = await FollowupAsync("Starting...").ConfigureAwait(false);
        var game = BlackJackService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}