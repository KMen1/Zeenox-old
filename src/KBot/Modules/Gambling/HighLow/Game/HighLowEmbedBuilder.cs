using System.Globalization;
using Discord;

namespace KBot.Modules.Gambling.HighLow.Game;

public class HighLowEmbedBuilder : EmbedBuilder
{
    public HighLowEmbedBuilder(HighLowGame game)
    {
        Title = $"Higher/Lower | {game.Id}";
        Description = $"**Original Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n" +
                      $"**Current Bet:** {game.Stake.ToString("N0", CultureInfo.InvariantCulture)} credits";
        Color = Discord.Color.Gold;
        ImageUrl = game.GetTablePicUrl();
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Higher",
            Value = $"Multiplier: **{game.HighMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x**\n" +
                    $"Prize: **{game.HighStake.ToString("N0", CultureInfo.InvariantCulture)} credits**",
            IsInline = true
        });
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Lower",
            Value = $"Multiplier: **{game.LowMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x**\n" +
                    $"Prize: **{game.LowStake.ToString("N0", CultureInfo.InvariantCulture)} credits**",
            IsInline = true
        });
    }
    public HighLowEmbedBuilder(HighLowGame game, string description)
    {
        Title = $"Higher/Lower | {game.Id}";
        Description = $"**Original Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n" +
                      $"**Current Bet:** {game.Stake.ToString("N0", CultureInfo.InvariantCulture)} credits\n{description}";
        Color = Discord.Color.Gold;
        ImageUrl = game.GetTablePicUrl();
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Higher",
            Value = $"Multiplier: **{game.HighMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x**\n" +
                    $"Prize: **{game.HighStake.ToString("N0", CultureInfo.InvariantCulture)} credits**",
            IsInline = true
        });
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Lower",
            Value = $"Multiplier: **{game.LowMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x**\n" +
                    $"Prize: **{game.LowStake.ToString("N0", CultureInfo.InvariantCulture)} credits**",
            IsInline = true
        });
    }
}