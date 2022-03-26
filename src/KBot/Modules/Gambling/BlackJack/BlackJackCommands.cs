using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackCommands : KBotModuleBase
{
    [SlashCommand("blackjack", "Hagyományos Blackjack, másnéven 21")]
    public async Task StartBlackJackAsync([MinValue(100), MaxValue(1000000)] int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Gambling.Balance < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = GamblingService.CreateBlackJackGame(Context.User, msg, bet);
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.Balance -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, -bet, "BL - Tétrakás"));
        }).ConfigureAwait(false);
        _ = Task.Run(async () => await game.StartAsync().ConfigureAwait(false));
    }
}
