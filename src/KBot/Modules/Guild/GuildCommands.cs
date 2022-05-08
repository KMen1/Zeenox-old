using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;

namespace KBot.Modules.Guild;

[DefaultMemberPermissions(GuildPermission.SendMessages)]
public class GuildCommands : SlashModuleBase
{
    [SlashCommand("profile", "Gets the profile of a user.")]
    public async Task GetProfileAsync(SocketGuildUser? user = null)
    {
        var sUser = user ?? (SocketGuildUser)Context.User;
        var dbUser = await Mongo.GetUserAsync(sUser).ConfigureAwait(false);

        var description = new StringBuilder();
        description.Append(
            CultureInfo.InvariantCulture,
            $"<:discord:868896375469924402> Joined Discord: <t:{sUser.CreatedAt.ToUnixTimeSeconds()}:R>\n"
        );
        description.Append(
            sUser.JoinedAt.HasValue
              ? $"🛡️ Joined Guild: <t:{sUser.JoinedAt.Value.ToUnixTimeSeconds()}:R>\n"
              : "🛡️ Joined Guild: `Unavailable`\n"
        );
        description.Append(
            CultureInfo.InvariantCulture,
            $":warning: Has {dbUser.WarnIds.Count} warnings\n"
        );
        description.Append(
            CultureInfo.InvariantCulture,
            $":money_with_wings: Made {dbUser.TransactionIds.Count} transactions\n"
        );
        description.Append(
            CultureInfo.InvariantCulture,
            $"♦ Played {dbUser.GamesPlayed} gambling games\n"
        );
        description.Append(
            dbUser.OsuId == 0
              ? "<:osu:864051085810991164> No osu! account linked\n"
              : $"<:osu:864051085810991164> [click for osu! profile](https://osu.ppy.sh/users/{dbUser.OsuId})"
        );

        var eb = new EmbedBuilder()
            .WithAuthor($"{sUser.Username}'s Profile")
            .WithColor(Color.LightGrey)
            .WithDescription(description.ToString())
            .AddField(
                "Leveling",
                $"**🆙 Level:** `{dbUser.Level.ToString("N0", CultureInfo.InvariantCulture)}`\n"
                    + $"**➡ XP:** `{dbUser.Xp.ToString("N0", CultureInfo.InvariantCulture)}/{dbUser.RequiredXp.ToString("N0", CultureInfo.InvariantCulture)}`\n"
            )
            .AddField(
                "Gambling",
                $"**🆙 Level:** `{dbUser.GambleLevel.ToString("N0", CultureInfo.InvariantCulture)} (play {dbUser.GambleLevelRequired.ToWords()} to level up)`\n"
                    + $"**♦ Minimum Bet:** `{dbUser.MinimumBet.ToString("N0", CultureInfo.InvariantCulture)} credits`\n"
                    + $"**💳 Balance:** `{dbUser.Balance.ToString("N0", CultureInfo.InvariantCulture)} credits`\n"
                    + $"**💰 Money Won:** `{dbUser.MoneyWon.ToString("N0", CultureInfo.InvariantCulture)} credits`\n"
                    + $"**💸 Money Lost:** `{dbUser.MoneyLost.ToString("N0", CultureInfo.InvariantCulture)} credits`\n"
                    + $"**📈 Winrate:** `{dbUser.WinRate.ToString(CultureInfo.InvariantCulture)}% (🏆{dbUser.Wins.ToString("N0", CultureInfo.InvariantCulture)} 🚫{dbUser.Losses.ToString("N0", CultureInfo.InvariantCulture)})`"
            )
            .WithThumbnailUrl(sUser.GetAvatarUrl())
            .WithFooter(
                $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
                Context.User.GetAvatarUrl()
            )
            .Build();
        await RespondAsync(embed: eb).ConfigureAwait(false);
    }
}
