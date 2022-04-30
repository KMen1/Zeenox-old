using System.Globalization;
using Discord;

namespace KBot.Modules.Gambling.Crash.Game;

public class CrashEmbedBuilder : EmbedBuilder
{
    public CrashEmbedBuilder(CrashGame game)
    {
        Title = $"Crash | {game.Id}";
        Description = $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits";
        Color = Discord.Color.Gold;
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Multiplier",
            Value = $"`{game.Multiplier.ToString("0.00", CultureInfo.InvariantCulture)}x`",
            IsInline = true
        });
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Profit",
            Value = $"`{game.Profit.ToString("N0", CultureInfo.InvariantCulture)}`",
            IsInline = true
        });
    }
    public CrashEmbedBuilder(CrashGame game, string description)
    {
        Title = $"Crash | {game.Id}";
        Description = $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n{description}";
        Color = Discord.Color.Gold;
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Multiplier",
            Value = $"`{game.Multiplier.ToString("0.00", CultureInfo.InvariantCulture)}x`",
            IsInline = true
        });
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Profit",
            Value = $"`{game.Profit.ToString("N0", CultureInfo.InvariantCulture)}`",
            IsInline = true
        });
    }
}