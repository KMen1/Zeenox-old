using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Extensions;

namespace KBot.Modules.Gambling.Mine;

[DefaultMemberPermissions(GuildPermission.SendMessages)]
[Group("mine", "Roobet Mine")]
public class MineCommands : SlashModuleBase
{
    private readonly MineService _minesService;

    public MineCommands(MineService minesService)
    {
        _minesService = minesService;
    }
    
    [SlashCommand("start", "Starts a new game of mine")]
    public async Task StartMinesAsync(
        [MinValue(100)] [MaxValue(1000000)] int bet,
        [MinValue(5)] [MaxValue(24)] int mines
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
        var game = _minesService.CreateGame((SocketGuildUser)Context.User, msg, bet, mines);
        await game.StartAsync().ConfigureAwait(false);
    }

    [SlashCommand("stop", "Stops the specified game")]
    public async Task StopMinesAsync(string id)
    {
        var game = _minesService.GetGame(id);
        var result = game.CheckIfInteractionIsPossible(Context.User.Id, out var eb);
        if (!result)
        {
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        if (!game!.CanStop)
        {
            var sEb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription(
                    "**You need to click at least one field to be able to stop the game.**"
                )
                .Build();
            await RespondAsync(embed: sEb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync(true).ConfigureAwait(false);
        var stopEb = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription("**Stopped.**")
            .Build();
        await game.StopAsync(false).ConfigureAwait(false);
        await FollowupAsync(embed: stopEb).ConfigureAwait(false);
    }
}
