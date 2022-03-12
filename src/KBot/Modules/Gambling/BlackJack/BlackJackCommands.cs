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
        dbUser.GamblingProfile ??= new GamblingProfile();
        dbUser.GamblingProfile.BlackJack ??= new BlackJackProfile();
        if (dbUser.GamblingProfile.Money < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        dbUser.GamblingProfile.Money -= bet;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = GamblingService.CreateBlackJackGame(Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}
