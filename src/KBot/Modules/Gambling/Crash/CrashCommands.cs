using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Models;

namespace KBot.Modules.Gambling.Crash;

public class CrashCommands : KBotModuleBase
{
    [SlashCommand("crash", "Szokásos crash játék.")]
    public async Task StartCrash([MinValue(100), MaxValue(1000000)]int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (dbUser.Gambling.Money < bet)
        {
            await FollowupAsync("Nincs elég 🪙KCoin-od ekkora tét rakásához.").ConfigureAwait(false);
            return;
        }
        var msg = await FollowupAsync("Létrehozás...").ConfigureAwait(false);
        var game = GamblingService.CreateCrashGame(Context.User, msg, bet);
        await Database.UpdateUserAsync(Context.Guild, Context.User, x =>
        {
            x.Gambling.Money -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, -bet, "CR - Tétrakás"));
        }).ConfigureAwait(false);
        await game.StartAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("crash:*")]
    public async Task StopCrash(string id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = GamblingService.GetCrashGame(id);
        if (Context.User.Id != game.User.Id)
            return;
        await GamblingService.StopCrashGameAsync(id).ConfigureAwait(false);
    }
}