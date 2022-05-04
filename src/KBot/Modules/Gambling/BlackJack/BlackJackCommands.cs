using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;

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
        var game = _blackJackService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}
