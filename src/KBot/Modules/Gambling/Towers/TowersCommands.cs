using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

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
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser) Context.User).ConfigureAwait(false);
        if (dbUser.Balance < bet)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Insufficient balance!**")
                .AddField("Balance", $"{dbUser.Balance.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .AddField("Bet", $"{bet.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        if (dbUser.MinimumBet > bet)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You must bet at least you minimum bet!**")
                .AddField("Minimum bet", $"{dbUser.MinimumBet.ToString("N0", CultureInfo.InvariantCulture)}", true)
                .AddField("Bet", $"{bet.ToString("N0", CultureInfo.InvariantCulture)}", true)
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
        var game = _towersService.CreateGame((SocketGuildUser) Context.User, msg, bet, diff);
        await game.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("stop", "Stops the specified game")]
    public async Task StopTowersGameAsync(string id)
    {
        var game = _towersService.GetGame(id);
        if (game is null)
        {
            var sEb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**No game found with that id.**")
                .Build();
            await RespondAsync(embed: sEb).ConfigureAwait(false);
            return;
        }

        if (game.User.Id != Context.User.Id)
        {
            var sEb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You can't stop another players game!**")
                .Build();
            await RespondAsync(embed: sEb).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription("**Stopped.**")
            .Build();
        await game.StopAsync().ConfigureAwait(false);
        await FollowupAsync(embed: eb).ConfigureAwait(false);
    }
}