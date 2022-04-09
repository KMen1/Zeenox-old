using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.HighLow;
public class HighLowCommands : KBotModuleBase
{
    public HighLowService HighLowService { get; set; }
    
    [SlashCommand("highlow", "Döntsd el hogy az osztónál lévő kártya nagyobb vagy kisebb a tiédnél.")]
    public async Task StartHighLowAsync([MinValue(100), MaxValue(1000000)]int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var (userHasEnough, guildHasEnough) = await Database.GetGambleValuesAsync(Context.Guild, Context.User, bet).ConfigureAwait(false);
        if (!userHasEnough)
        {
            await FollowupAsync("Nincs elég pénzed ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        if (!guildHasEnough)
        {
            await FollowupAsync("Nincs elég pénz a kasszában ekkor tét rakásához.").ConfigureAwait(false);
            return;
        }
        
        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = HighLowService.CreateGame(Context.User, msg, bet);
        
        _ = Task.Run(async () => await UpdateUserAsync(Context.User, x =>
        {
            x.Gambling.Balance -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, -bet, "HL - Tétrakás"));
        }).ConfigureAwait(false));
        _ = Task.Run(async () => await UpdateUserAsync(BotUser, x => x.Money += bet).ConfigureAwait(false));
        _ = Task.Run(async () => await game.StartAsync().ConfigureAwait(false));
    }
}