using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using KBot.Models;

namespace KBot.Modules.Leveling;

[Group("level", "Leveling system commands")]
public class Levels : SlashModuleBase
{
    [SlashCommand("rank", "Gets a users level")]
    public async Task GetLevelAsync(SocketUser user = null)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var setUser = user ?? Context.User;
        if (setUser.IsBot)
        {
            await FollowupAsync("You can't check the rank of a bot.").ConfigureAwait(false);
            return;
        }

        var dbUser = await Mongo.GetUserAsync((SocketGuildUser)setUser).ConfigureAwait(false);
        var level = dbUser.Level;
        var requiredXP = Math.Pow(level * 4, 2);
        var xp = dbUser.Xp;
        var embed = new EmbedBuilder()
            .WithAuthor(setUser.Username, setUser.GetAvatarUrl())
            .WithColor(Color.Gold)
            .WithDescription(
                $"**XP: **`{xp.ToString()}/{requiredXP}` ({dbUser.TotalXp} Total) \n**Level: **`{level.ToString()}`")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("top", "Sends the top 10 users")]
    public async Task GetTopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var top = await Mongo.GetTopUsersAsync(Context.Guild, 10).ConfigureAwait(false);

        var userColumn = "";
        var levelColumn = "";

        foreach (var user in top)
        {
            userColumn += $"{top.IndexOf(user) + 1}. {Context.Guild.GetUser(user.UserId).Mention}\n";
            levelColumn += $"{user.Level} ({user.Xp} XP)\n";
        }

        await FollowupAsync(embed: new EmbedBuilder()
            .WithTitle("Top 10 Users")
            .WithColor(Color.Green)
            .AddField("User", userColumn, true)
            .AddField("Level", levelColumn, true)
            .Build(), ephemeral: true).ConfigureAwait(false);
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
            var xp = new Random().Next(100, 500);
            await Mongo.UpdateUserAsync(Context.Guild, Context.User, x =>
            {
                x.DailyXpClaim = DateTime.UtcNow;
                x.Xp += xp;
            }).ConfigureAwait(false);
            await FollowupWithEmbedAsync(Color.Green, $"Successfully collected your daily XP of {xp}", "",
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

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changexp", "Change someone's XP")]
    public async Task ChangeXPAsync(SocketUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.UpdateUserAsync(Context.Guild, user, x => x.Xp += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "XP set!",
            $"{user.Mention} now has an XP of **{dbUser.Xp.ToString()}**").ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("changelevel", "Change someone's level")]
    public async Task ChangeLevelAsync(SocketUser user, int offset)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Mongo.UpdateUserAsync(Context.Guild, user, x => x.Level += offset).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green, "Level set!",
            $"{user.Mention} now has a level of **{dbUser.Level.ToString()}**").ConfigureAwait(false);
    }
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setchannel", "Set the channel for level up messages")]
    public async Task SetChannelAsync(ITextChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LevelUpChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setafk", "Set the AFK channel")]
    public async Task SetAfkChannelAsync(IVoiceChannel channel)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.AfkChannelId = channel.Id)
            .ConfigureAwait(false);
        await RespondAsync("Channel set!", ephemeral: true).ConfigureAwait(false);
    }
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("addrole", "Add a role to the leveling roles")]
    public async Task AddRoleAsync(IRole role, [MinValue(1)] int level)
    {
        await Mongo
            .UpdateGuildConfigAsync(Context.Guild, x => x.LevelRoles.Add(new LevelRole(role.Id, level)))
            .ConfigureAwait(false);
        await RespondAsync("Role Added!", ephemeral: true).ConfigureAwait(false);
    }
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("removerole", "Remove a role from the leveling roles")]
    public async Task RemoveRoleAsync(IRole role)
    {
        await Mongo.UpdateGuildConfigAsync(Context.Guild, x => x.LevelRoles.RemoveAll(y => y.Id == role.Id))
            .ConfigureAwait(false);
        await RespondAsync("Role Removed", ephemeral: true).ConfigureAwait(false);
    }
}