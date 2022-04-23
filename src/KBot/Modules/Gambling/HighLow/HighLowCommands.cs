using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowCommands : SlashModuleBase
{
    private readonly HighLowService _highLowService;
    public HighLowCommands(HighLowService highLowService)
    {
        _highLowService = highLowService;
    }

    [SlashCommand("highlow", "Starts a new game of higher/lower.")]
    public async Task StartHighLowAsync([MinValue(100)] [MaxValue(1000000)] int bet)
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
        var game = _highLowService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}