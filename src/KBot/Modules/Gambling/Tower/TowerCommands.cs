using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;
using KBot.Modules.Gambling.Tower.Game;

namespace KBot.Modules.Gambling.Tower;

[Group("tower", "Roobet Towers")]
public class TowerCommands : SlashModuleBase
{
    private readonly TowerService _towersService;

    public TowerCommands(TowerService towersService)
    {
        _towersService = towersService;
    }

    [SlashCommand("start", "Starts a new game of towers")]
    public async Task CreateTowersGameAsync(
        [MinValue(100)] [MaxValue(1000000)] int bet,
        Difficulty diff
    )
    {
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        var result = dbUser.CanStartGame(bet, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var sEb = new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithDescription("**Starting Game...**")
            .Build();
        await RespondAsync(embed: sEb).ConfigureAwait(false);
        var msg = await GetOriginalResponseAsync().ConfigureAwait(true);
        var game = _towersService.CreateGame((SocketGuildUser)Context.User, msg, bet, diff);
        await game.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("stop", "Stops the specified game")]
    public async Task StopTowersGameAsync(string id)
    {
        var game = _towersService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync(true).ConfigureAwait(false);
        var stopEb = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription("**Stopped.**")
            .Build();
        await game!.StopAsync().ConfigureAwait(false);
        await FollowupAsync(embed: stopEb).ConfigureAwait(false);
    }
}
