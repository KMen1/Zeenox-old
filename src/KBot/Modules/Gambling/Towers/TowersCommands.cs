using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.Towers;

[Group("towers", "Roobet towers játék")]
public class TowersCommands : KBotModuleBase
{
    [SlashCommand("start", "Towers indítása")]
    public async Task CreateTowersGameAsync([MinValue(100), MaxValue(1000000)] int bet, Difficulty diff)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Gambling.Balance < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }

        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = GamblingService.CreateTowersGame(Context.User, msg, bet, diff);

        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.Balance -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, bet, "TW - Tétrakás"));
        }).ConfigureAwait(false);
        _ = Task.Run(async () => await game.StartAsync().ConfigureAwait(false));
    }

    [SlashCommand("stop", "Towers leállítása")]
    public async Task StopTowersGameAsync(string id)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var game = GamblingService.GetTowersGame(id);
        if (game is null)
        {
            await FollowupAsync("Nem található ilyen id-jű játék").ConfigureAwait(false);
            return;
        }
        if (game.User.Id != Context.User.Id)
            return;
        var reward = await game.StopAsync().ConfigureAwait(false);
        if (reward != 0)
        {
            await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
            {
                x.Gambling.Balance += reward;
                x.Gambling.MoneyWon += reward;
                x.Gambling.Wins++;
                x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, reward, "TW - WIN"));
            }).ConfigureAwait(false);
            return;
        }
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.MoneyLost += game.Bet;
            x.Gambling.Losses++;
        }).ConfigureAwait(false);
        await FollowupAsync("Leállítva").ConfigureAwait(false);
    }

}