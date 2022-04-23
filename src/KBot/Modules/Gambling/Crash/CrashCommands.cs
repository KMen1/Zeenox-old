using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.Crash;

public class CrashCommands : SlashModuleBase
{
    private readonly CrashService _crashService;
    public CrashCommands(CrashService crashService)
    {
        _crashService = crashService;
    }

    [SlashCommand("crash", "Starts a new game of crash.")]
    public async Task StartCrashGameAsync([MinValue(100)] [MaxValue(1000000)] int bet)
    {
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            await RespondAsync("Insufficient balance.").ConfigureAwait(false);
            return;
        }

        if (dbUser.MinimumBet > bet)
        {
            await RespondAsync($"You must bet at least {dbUser.MinimumBet} credits.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await RespondAsync("Starting Game...").ConfigureAwait(false);
        var msg = await GetOriginalResponseAsync().ConfigureAwait(true);
        var game = _crashService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("crash:*")]
    public async Task StopCrashGameAsync(string id)
    {
        await DeferAsync().ConfigureAwait(false);
        var game = _crashService.GetGame(id);
        if (Context.User.Id != game.User.Id)
            return;
        await _crashService.StopGameAsync(id).ConfigureAwait(false);
    }
}