using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.Mines;

[Group("mine", "Roobet Mine-hoz hasonló játék")]
public class MineCommands : KBotModuleBase
{
    [SlashCommand("start", "Elindít egy új játékot")]
    public async Task StartMinesAsync([MinValue(100), MaxValue(1000000)]int bet, [MinValue(5), MaxValue(24)]int mines)
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
        var game = GamblingService.CreateMinesGame(Context.User, msg, bet, mines);
        
        _ = Task.Run(async () => await UpdateUserAsync(Context.User, x =>
        {
            x.Gambling.Balance -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, -bet, "MN - Tétrakás"));
        }).ConfigureAwait(false));
        _ = Task.Run(async () => await UpdateUserAsync(BotUser, x => x.Money += bet).ConfigureAwait(false));
        _ = Task.Run(async () => await game.StartAsync().ConfigureAwait(false));
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
        if (game.User.Id != Context.User.Id)
            await FollowupAsync("Ez nem a te játékod!").ConfigureAwait(false);
        if (!game.CanStop)
        {
            await FollowupAsync("Egy mezőt meg kell nyomnod mielőtt kiszállhatnál.").ConfigureAwait(false);
            return;
        }
        var reward = await game.StopAsync().ConfigureAwait(false);
        if (reward is { } i)
        {
            _ = Task.Run(async () => await UpdateUserAsync(Context.User, x =>
            {
                x.Gambling.Balance += i;
                x.Gambling.MoneyWon += i - game.Bet;
                x.Gambling.Wins++;
                x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, i, "MN - WIN"));
            }).ConfigureAwait(false));
            _ = Task.Run(async () => await UpdateUserAsync(BotUser, x => x.Money -= i).ConfigureAwait(false));
        }
        else
        {
            _ = Task.Run(async () => await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
            {
                x.Gambling.MoneyLost += game.Bet;
                x.Gambling.Losses++;
            }).ConfigureAwait(false));
        }
        await FollowupAsync("Leállítva").ConfigureAwait(false);
    }
}