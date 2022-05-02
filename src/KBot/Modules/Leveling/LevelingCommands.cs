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

[Group("level", "Leveling system commands")]
public class LevelingCommands : SlashModuleBase
{
    [SlashCommand("rank", "Gets a users level")]
    public async Task GetLevelAsync(SocketUser? user = null)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var setUser = user ?? Context.User;
        if (setUser.IsBot)
        {
            await FollowupAsync("You can't check the rank of a bot.").ConfigureAwait(false);
            return;
        }

        var dbUser = await Mongo.GetUserAsync((SocketGuildUser) setUser).ConfigureAwait(false);
        var embed = new EmbedBuilder()
            .WithAuthor(setUser.Username, setUser.GetAvatarUrl())
            .WithColor(Color.Gold)
            .AddField("🆙 Level", $"`{dbUser.Level.ToString(CultureInfo.InvariantCulture)}`")
            .AddField("➡ XP/Required",
                $"`{dbUser.Xp.ToString("N0", CultureInfo.InvariantCulture)}/{dbUser.RequiredXp.ToString("N0", CultureInfo.InvariantCulture)}`")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("top", "Sends the top 10 users")]
    public async Task GetTopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var top = (await Mongo.GetTopUsersAsync(Context.Guild, 10).ConfigureAwait(false)).ToList();

        var userColumn = new StringBuilder();
        var levelColumn = new StringBuilder();
        foreach (var user in top)
        {
            userColumn.Append(CultureInfo.InvariantCulture, $"{top.IndexOf(user) + 1}. {Context.Guild.GetUser(user.UserId).Mention}\n");
            levelColumn.Append(CultureInfo.InvariantCulture, $"{user.Level}\n");
        }

        await FollowupAsync(embed: new EmbedBuilder()
            .WithTitle("Top 10 Users")
            .WithColor(Color.Green)
            .AddField("User", userColumn.ToString(), true)
            .AddField("Level", levelColumn.ToString(), true)
            .Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("daily", "Collects your daily XP")]
    public async Task GetDailyAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.GetUserAsync((SocketGuildUser) Context.User).ConfigureAwait(false);
        var lastDaily = dbUser.DailyXpClaim;
        var canClaim = lastDaily.AddDays(1) < DateTime.UtcNow;
        if (lastDaily == DateTime.MinValue || canClaim)
        {
            var xp = RandomNumberGenerator.GetInt32(1000, 5000);
            await Mongo.UpdateUserAsync((SocketGuildUser) Context.User, x =>
            {
                x.DailyXpClaim = DateTime.UtcNow;
                x.Xp += xp;
            }).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green,
                $"Successfully collected your daily XP of {xp.ToString("N0", CultureInfo.InvariantCulture)}", "",
                ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            var timeLeft = lastDaily.AddDays(1) - DateTime.UtcNow;
            await FollowupWithEmbedAsync(Color.Green, "Unable to collect",
                    $"Come back in {timeLeft.Humanize()}", ephemeral: true)
                .ConfigureAwait(false);
        }
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("changexp", "Change someone's XP")]
    public async Task ChangeXpAsync(SocketGuildUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.UpdateUserAsync(user, x => x.Xp += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "XP set!",
                $"{user.Mention} now has an XP of **{dbUser.Xp.ToString("N0", CultureInfo.InvariantCulture)}**")
            .ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("changelevel", "Change someone's level")]
    public async Task ChangeLevelAsync(SocketGuildUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.UpdateUserAsync(user, x => x.Level += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Level set!",
                $"{user.Mention} now has a level of **{dbUser.Level.ToString(CultureInfo.InvariantCulture)}**")
            .ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setchannel", "Set the channel for level up messages")]
    public async Task SetChannelAsync(ITextChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LevelUpChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**Leveling system is now disabled**"
                : $"**Level up messages will be sent to {channel.Mention}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setafk", "Set the AFK channel")]
    public async Task SetAfkChannelAsync(IVoiceChannel? channel = null)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.AfkChannelId = channel?.Id ?? 0)
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(channel is null ? Color.Red : Color.Green)
            .WithDescription(channel is null
                ? "**AFK Channel disabled**"
                : $"**{channel.Mention} is set to be the AFK channel**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("addrole", "Add a role to the leveling roles")]
    public async Task AddRoleAsync(IRole role, [MinValue(1)] int level)
    {
        await Mongo
            .UpdateGuildConfigAsync(Context.Guild, x => x.LevelRoles.Add(new LevelRole(role.Id, level)))
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"**{role.Mention} will now be granted after reaching level {level}**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("removerole", "Remove a role from the leveling roles")]
    public async Task RemoveRoleAsync(IRole role)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LevelRoles.RemoveAll(y => y.Id == role.Id))
            .ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription($"**{role.Mention} removed from level roles**")
            .Build();
        await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
    }
}