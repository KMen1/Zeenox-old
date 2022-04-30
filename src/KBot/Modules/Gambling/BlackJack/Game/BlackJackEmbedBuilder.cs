using System.Globalization;
using Discord;

namespace KBot.Modules.Gambling.BlackJack.Game;

public class BlackJackEmbedBuilder : EmbedBuilder
{
    public BlackJackEmbedBuilder(BlackJackGame game)
    {
        Title = $"Blackjack | {game.Id}";
        Description = $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits";
        Color = Discord.Color.Gold;
        ImageUrl = game.GetTablePicUrl();
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Player",
            Value = $"Value: `{game.PlayerScore.ToString(CultureInfo.InvariantCulture)}`",
            IsInline = true
        });
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Dealer",
            Value = game.Hidden ? "Value: `?`" : $"Value: `{game.DealerScore.ToString(CultureInfo.InvariantCulture)}`",
            IsInline = true
        });
    }
    public BlackJackEmbedBuilder(BlackJackGame game, string description)
    {
        Title = $"Blackjack | {game.Id}";
        Description = $"**Bet:** {game.Bet.ToString("N0", CultureInfo.InvariantCulture)} credits\n{description}";
        Color = Discord.Color.Gold;
        ImageUrl = game.GetTablePicUrl();
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Player",
            Value = $"Value: `{game.PlayerScore.ToString(CultureInfo.InvariantCulture)}`",
            IsInline = true
        });
        Fields.Add(new EmbedFieldBuilder
        {
            Name = "Dealer",
            Value = game.Hidden ? "Value: `?`" : $"Value: `{game.DealerScore.ToString(CultureInfo.InvariantCulture)}`",
            IsInline = true
        });
    }
}