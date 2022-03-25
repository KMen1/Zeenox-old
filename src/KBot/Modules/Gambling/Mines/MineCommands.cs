using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Gambling.Mines;

[Group("mine", "Roobet Mine-hoz hasonló játék")]
public class MineCommands : KBotModuleBase
{
    [SlashCommand("start", "Elindít egy új játékot")]
    public async Task StartMinesAsync([MinValue(100), MaxValue(1000000)]int bet, [MinValue(5), MaxValue(24)]int mines)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Gambling.Money < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        
        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = GamblingService.CreateMinesGame(Context.User, msg, bet, mines);
        
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.Money -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, bet, "MN - Tétrakás"));
        }).ConfigureAwait(false);
        await game.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("stop", "Leállítja a játékot")]
    public async Task StopMinesAsync(string id)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var game = GamblingService.GetMinesGame(id);
        if (game is null)
        {
            await FollowupAsync("Nem található ilyen id-jű játék").ConfigureAwait(false);
            return;
        }
        if (!game.CanStop)
        {
            await FollowupAsync("Egy mezőt meg kell nyomnod mielőtt kiszállhatnál.").ConfigureAwait(false);
            return;
        }
        var reward = await game.StopAsync().ConfigureAwait(false);
        if (reward is { } i)
        {
            await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
            {
                x.Gambling.Money += i;
                x.Gambling.MoneyWon += i;
                x.Gambling.Wins++;
                x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, i, "MN - WIN"));
            }).ConfigureAwait(false);
        }
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.MoneyLost += game.Bet;
            x.Gambling.Losses++;
        }).ConfigureAwait(false);
        await FollowupAsync("Leállítva").ConfigureAwait(false);
    }
}