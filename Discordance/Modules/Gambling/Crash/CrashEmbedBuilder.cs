﻿using System.Globalization;
using Discord;

namespace Discordance.Modules.Gambling.Crash;

public class CrashEmbedBuilder : EmbedBuilder
{
    public CrashEmbedBuilder(CrashGame game, string? description = null)
    {
        Title = "Crash";
        Description =
            $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits"
            + (description is null ? "" : $"\n{description}");
        Color = Discord.Color.Gold;
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "Multiplier",
                Value = $"`{game.Multiplier.ToString("0.00", CultureInfo.InvariantCulture)}x`",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "Profit",
                Value = $"`{game.Profit.ToString("N0", CultureInfo.InvariantCulture)}`",
                IsInline = true
            }
        );
    }
}
