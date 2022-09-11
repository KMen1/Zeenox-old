using System.Globalization;
using Discord;

namespace Discordance.Modules.Gambling.BlackJack;

public class BlackJackEmbedBuilder : EmbedBuilder
{
    public BlackJackEmbedBuilder(BlackJackGame game, string? description = null)
    {
        Title = "Blackjack";
        Description =
            $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits"
            + (description is null ? "" : $"\n{description}");
        Color = Discord.Color.Gold;
        ImageUrl = game.GetTablePicUrl();
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = $"Player - {game.PlayerScore.ToString()}",
                Value = "\u200b",
                IsInline = true
            }
        );
        Fields.Add(
            new EmbedFieldBuilder
            {
                Name = "Dealer - {}".Replace(
                    "{}",
                    game.Hidden
                      ? "?"
                      : $"{game.DealerScore.ToString(CultureInfo.InvariantCulture)}",
                    System.StringComparison.OrdinalIgnoreCase
                ),
                Value = "\u200b",
                IsInline = true
            }
        );
    }
}
