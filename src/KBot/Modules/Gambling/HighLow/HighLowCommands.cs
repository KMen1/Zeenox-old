using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Gambling.HighLow;
public class HighLowCommands : KBotModuleBase
{
    [SlashCommand("highlow", "Döntsd el hogy az osztónál lévő kártya nagyobb vagy kisebb a tiédnél.")]
    public async Task StartHighLowAsync([MinValue(100), MaxValue(1000000)]int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Gambling.Money < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.", ephemeral:true).ConfigureAwait(false);
            return;
        }
        
        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = GamblingService.CreateHighLowGame(Context.User, msg, bet);
        
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.Money -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, -bet, "HL - Tétrakás"));
        }).ConfigureAwait(false);
        
        await game.StartAsync().ConfigureAwait(false);
    }
}