using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;

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
        var game = _crashService.CreateGame((SocketGuildUser)Context.User, msg, bet);
        await game.StartAsync().ConfigureAwait(false);
    }
}
