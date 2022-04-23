using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.Towers;

[Group("towers", "Roobet Towers")]
public class TowersCommands : SlashModuleBase
{
    private readonly TowersService _towersService;
    public TowersCommands(TowersService towersService)
    {
        _towersService = towersService;
    }

    [SlashCommand("start", "Starts a new game of towers")]
    public async Task CreateTowersGameAsync([MinValue(100)] [MaxValue(1000000)] int bet, Difficulty diff)
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
        var game = _towersService.CreateGame((SocketGuildUser)Context.User, msg, bet, diff);
        await game.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("stop", "Stops the specified game")]
    public async Task StopTowersGameAsync(string id)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var game = _towersService.GetGame(id);
        if (game is null)
        {
            await FollowupAsync("No game found for that id.").ConfigureAwait(false);
            return;
        }

        if (game.User.Id != Context.User.Id)
            return;
        await game.StopAsync().ConfigureAwait(false);
        await FollowupAsync("Stopped!").ConfigureAwait(false);
    }
}