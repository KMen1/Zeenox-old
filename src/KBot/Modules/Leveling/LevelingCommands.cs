using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Models;

namespace KBot.Modules.Leveling;

[DefaultMemberPermissions(GuildPermission.SendMessages)]
[Group("level", "Leveling system commands")]
public class LevelingCommands : SlashModuleBase
{
    [SlashCommand("top", "Sends the top 10 users")]
    public async Task GetTopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var top = (await Mongo.GetTopUsersAsync(Context.Guild, 10).ConfigureAwait(false)).ToList();

        var userColumn = new StringBuilder();
        var levelColumn = new StringBuilder();
        foreach (var user in top)
        {
            userColumn.Append(
                CultureInfo.InvariantCulture,
                $"{top.IndexOf(user) + 1}. {Context.Guild.GetUser(user.UserId).Mention}\n"
            );
            levelColumn.Append(CultureInfo.InvariantCulture, $"{user.Level}\n");
        }

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle("Top 10 Users")
                    .WithColor(Color.Green)
                    .AddField("User", userColumn.ToString(), true)
                    .AddField("Level", levelColumn.ToString(), true)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [SlashCommand("daily", "Collects your daily XP")]
    public async Task GetDailyAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)Context.User).ConfigureAwait(false);
        var lastDaily = dbUser.DailyXpClaim;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var xp = RandomNumberGenerator.GetInt32(1000, 5000);
            await Mongo
                .UpdateUserAsync(
                    (SocketGuildUser)Context.User,
                    x =>
                    {
                        x.DailyXpClaim = DateTime.UtcNow;
                        x.Xp += xp;
                    }
                )
                .ConfigureAwait(false);
            await FollowupWithEmbedAsync(
                    Color.Green,
                    $"Successfully collected your daily XP of {xp.ToString("N0", CultureInfo.InvariantCulture)}",
                    "",
                    ephemeral: true
                )
                .ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
            await FollowupWithEmbedAsync(
                    Color.Green,
                    "Unable to collect",
                    $"Come back in {timeLeft.Humanize()}",
                    ephemeral: true
                )
                .ConfigureAwait(false);
        }
    }
}

public class AdminCommands : SlashModuleBase
{
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("changexp", "Change someone's XP")]
    public async Task ChangeXpAsync(SocketGuildUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.UpdateUserAsync(user, x => x.Xp += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(
                Color.Green,
                "XP set!",
                $"{user.Mention} now has an XP of **{dbUser.Xp.ToString("N0", CultureInfo.InvariantCulture)}**"
            )
            .ConfigureAwait(false);
    }

    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [SlashCommand("changelevel", "Change someone's level")]
    public async Task ChangeLevelAsync(SocketGuildUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo
            .UpdateUserAsync(user, x => x.Level += offset)
            .ConfigureAwait(false);
        await FollowupWithEmbedAsync(
                Color.Green,
                "Level set!",
                $"{user.Mention} now has a level of **{dbUser.Level.ToString(CultureInfo.InvariantCulture)}**"
            )
            .ConfigureAwait(false);
    }
}
