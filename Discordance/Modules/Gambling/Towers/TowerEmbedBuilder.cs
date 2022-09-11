using System.Globalization;
using Discord;

namespace Discordance.Modules.Gambling.Towers;

public class TowerEmbedBuilder : EmbedBuilder
{
    public TowerEmbedBuilder(TowerGame game, string? description = null)
    {
        Title = "Towers";
        Description =
            $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n"
            + $"**Difficulty:** {game.Difficulty.ToString()}"
            + (description is null ? "" : $"\n{description}");
        Color = Discord.Color.Gold;
    }
}
