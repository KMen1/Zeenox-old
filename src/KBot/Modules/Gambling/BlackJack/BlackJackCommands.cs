using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Gambling.BlackJack;

public class BlackJackCommands : SlashModuleBase
{
    private readonly BlackJackService _blackJackService;

    public BlackJackCommands(BlackJackService blackJackService)
    {
        _blackJackService = blackJackService;
    }

    [SlashCommand("blackjack", "Starts a new game of Blackjack")]
    public async Task StartBlackJackAsync([MinValue(1)] [MaxValue(10000000)] int bet)
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
        var game = _blackJackService.CreateGame((SocketGuildUser) Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}