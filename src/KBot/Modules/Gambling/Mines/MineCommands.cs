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
        dbUser.GamblingProfile ??= new GamblingProfile();
        dbUser.GamblingProfile.Mines ??= new MinesProfile();
        if (dbUser.GamblingProfile.Money < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        dbUser.GamblingProfile.Money -= bet;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = GamblingService.CreateMinesGame(Context.User, msg, bet, mines);
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
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        var reward = await game.StopAsync().ConfigureAwait(false);
        if (reward is { } i)
        {
            dbUser.GamblingProfile.Money += i;
            dbUser.GamblingProfile.Mines.MoneyWon += i;
            dbUser.GamblingProfile.Mines.Wins++;
            
        }

        dbUser.GamblingProfile.Mines.Losses++;
        dbUser.GamblingProfile.Mines.MoneyLost += game.Bet;
        await Database.UpdateUserAsync(Context.Guild.Id, dbUser).ConfigureAwait(false);
        await FollowupAsync("Leállítva").ConfigureAwait(false);
    }
}