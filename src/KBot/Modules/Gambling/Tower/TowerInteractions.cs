using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Gambling.Tower;

public class TowerInteractions : SlashModuleBase
{
    private readonly TowerService _towersService;

    public TowerInteractions(TowerService towersService)
    {
        _towersService = towersService;
    }

    [ComponentInteraction("towers:*:*:*")]
    public async Task ClickFieldAsync(string id, int x, int y)
    {
        var game = _towersService.GetGame(id);
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