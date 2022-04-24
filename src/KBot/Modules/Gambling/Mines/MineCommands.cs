using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Models;

namespace KBot.Modules.Gambling.Mines;

[Group("mine", "Roobet Mine")]
public class MineCommands : SlashModuleBase
{
    private readonly MinesService _minesService;
    public MineCommands(MinesService minesService)
    {
        _minesService = minesService;
    }

    [SlashCommand("start", "Starts a new game of mine")]
    public async Task StartMinesAsync([MinValue(100)] [MaxValue(1000000)] int bet,
        [MinValue(5)] [MaxValue(24)] int mines)
    {
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Insufficient balance!**")
                .AddField("Balance", $"{dbUser.Balance.ToString(CultureInfo.InvariantCulture)}", true)
                .AddField("Bet", $"{bet.ToString(CultureInfo.InvariantCulture)}", true)
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        if (dbUser.MinimumBet > bet)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You must bet at least you minimum bet!**")
                .AddField("Minimum bet", $"{dbUser.MinimumBet.ToString(CultureInfo.InvariantCulture)}", true)
                .AddField("Bet", $"{bet.ToString(CultureInfo.InvariantCulture)}", true)
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var sEb = new EmbedBuilder()
            .WithColor(Color.Orange)
            .WithDescription("**Starting Game...**")
            .Build();
        await RespondAsync(embed: sEb).ConfigureAwait(false);
        var msg = await GetOriginalResponseAsync().ConfigureAwait(true);
        var game = _minesService.CreateGame((SocketGuildUser)Context.User, msg, bet, mines);
        await game.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("stop", "Stops the specified game")]
    public async Task StopMinesAsync(string id)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var game = _minesService.GetGame(id);
        if (game is null)
        {
            await FollowupAsync("No game found for that id.").ConfigureAwait(false);
            return;
        }

        if (game.User.Id != Context.User.Id)
            await FollowupAsync("You can't stop another players game!").ConfigureAwait(false);
        if (!game.CanStop)
        {
            await FollowupAsync("You need to click at least one filed to be able to stop the game.")
                .ConfigureAwait(false);
            return;
        }

        await game.StopAsync(false).ConfigureAwait(false);
        await FollowupAsync("Stopped!").ConfigureAwait(false);
    }
}