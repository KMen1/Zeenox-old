using System.Globalization;
using Discord;

namespace KBot.Modules.Gambling.Tower.Game;

public class TowerEmbedBuilder : EmbedBuilder
{
    public TowerEmbedBuilder(TowerGame game)
    {
        Title = $"Towers | {game.Id}";
        Description = $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n" +
                      $"**Difficulty:** {game.Difficulty.ToString()}";
        Color = Discord.Color.Gold;
    }
    public TowerEmbedBuilder(TowerGame game, string description)
    {
        Title = $"Towers | {game.Id}";
        Description = $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n" +
                      $"**Difficulty:** {game.Difficulty.ToString()}\n{description}";
        Color = Discord.Color.Gold;
    }
}