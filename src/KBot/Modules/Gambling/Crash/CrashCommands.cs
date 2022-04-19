using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.Crash;

public class CrashCommands : SlashModuleBase
{
    public CrashService CrashService { get; set; }

    [SlashCommand("crash", "Starts a new game of crash.")]
    public async Task StartCrashGameAsync([MinValue(100)] [MaxValue(1000000)] int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            await FollowupAsync("Insufficient balance.").ConfigureAwait(false);
            return;
        }

        var msg = await FollowupAsync("Starting...").ConfigureAwait(false);
        var game = CrashService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
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