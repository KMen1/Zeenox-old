using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.HighLow;

public class HighLowCommands : SlashModuleBase
{
    public HighLowService HighLowService { get; set; }

    [SlashCommand("highlow", "Starts a new game of higher/lower.")]
    public async Task StartHighLowAsync([MinValue(100)] [MaxValue(1000000)] int bet)
    {
        await DeferAsync().ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            await FollowupAsync("Insufficient balance.").ConfigureAwait(false);
            return;
        }

        var msg = await FollowupAsync("Starting...").ConfigureAwait(false);
        var game = HighLowService.CreateGame(Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}