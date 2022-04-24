using System.Globalization;
using System.Threading.Tasks;
using Discord;
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
        var game = _highLowService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}