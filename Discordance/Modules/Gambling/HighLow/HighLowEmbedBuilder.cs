using System.Globalization;
using Discord;

namespace Discordance.Modules.Gambling.HighLow;

public class HighLowEmbedBuilder : EmbedBuilder
{
    public HighLowEmbedBuilder(HighLowGame game, string? description = null)
    {
        Title = "Higher/Lower";
        Description =
            $"**Original Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n"
            + $"**Current Bet:** {game.Stake.ToString("N0", CultureInfo.InvariantCulture)} credits"
            + (description is null ? "" : $"\n{description}");
        Color = Discord.Color.Gold;
        ImageUrl = game.GetTablePicUrl();
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name =
                    $"Higher - {game.HighMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x",
                Value =
                    $"Prize: **{game.HighStake.ToString("N0", CultureInfo.InvariantCulture)} credits**",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name =
                    $"Lower - {game.LowMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x",
                Value =
                    $"Prize: **{game.LowStake.ToString("N0", CultureInfo.InvariantCulture)} credits**",
                IsInline = true
            }
        );
    }
}
