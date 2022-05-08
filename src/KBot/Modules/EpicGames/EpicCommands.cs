using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Extensions;

namespace KBot.Modules.EpicGames;

public class EpicCommands : SlashModuleBase
{
    private readonly EpicGamesService _epicGamesService;

    public EpicCommands(EpicGamesService epicGamesService)
    {
        _epicGamesService = epicGamesService;
    }

    [DefaultMemberPermissions(GuildPermission.SendMessages)]
    [SlashCommand("freegames", "Send the current free games on the Epic Games Store.")]
    public async Task GetEpicFreeGameAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await FollowupAsync(embeds: _epicGamesService.ChachedGames.ToEmbedArray())
            .ConfigureAwait(false);
    }
}
