using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Gambling.Mine;

public class MineInteractions : SlashModuleBase
{
    private readonly MineService _minesService;

    public MineInteractions(MineService minesService)
    {
        _minesService = minesService;
    }

    [ComponentInteraction("mine:*:*:*")]
    public async Task HandleMineAsync(string id, int x, int y)
    {
        var game = _minesService.GetGame(id);
        if (game is null)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**Game not found!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        if (game.User.Id != Context.User.Id)
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**This is not your game!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await game.ClickFieldAsync(x, y).ConfigureAwait(false);
    }
}