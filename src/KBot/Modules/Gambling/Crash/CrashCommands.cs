using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Enums;
using KBot.Models.User;

namespace KBot.Modules.Gambling.Crash;

public class CrashCommands : SlashModuleBase
{
    public CrashService CrashService { get; set; }

    [SlashCommand("crash", "Starts a new game of crash.")]
    public async Task StartCrashGameAsync([MinValue(100)] [MaxValue(1000000)] int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var (userHasEnough, guildHasEnough) =
            await Database.GetGambleValuesAsync(Context.Guild, Context.User, bet).ConfigureAwait(false);
        if (!userHasEnough)
        {
            await FollowupAsync("Insufficient balance.").ConfigureAwait(false);
            return;
        }

        if (!guildHasEnough)
        {
            await FollowupAsync("Insufficient guild balance.").ConfigureAwait(false);
            return;
        }

        var msg = await FollowupAsync("Starting...").ConfigureAwait(false);
        var game = CrashService.CreateGame(Context.User, msg, bet);
        _ = Task.Run(async () => await UpdateUserAsync(Context.User, x =>
        {
            x.Money -= bet;
            x.Transactions.Add(new Transaction(game.Id, TransactionType.Gambling, -bet, "CR - Bet"));
        }).ConfigureAwait(false));
        _ = Task.Run(async () => await UpdateUserAsync(BotUser, x => x.Money += bet).ConfigureAwait(false));
        _ = Task.Run(async () => await game.StartAsync().ConfigureAwait(false));
    }

    [ComponentInteraction("crash:*")]
    public async Task StopCrashGameAsync(string id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = CrashService.GetGame(id);
        if (Context.User.Id != game.User.Id)
            return;
        await CrashService.StopGameAsync(id).ConfigureAwait(false);
    }
}