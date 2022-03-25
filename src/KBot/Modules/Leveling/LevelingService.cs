using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using KBot.Models;
using KBot.Services;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KBot.Modules.Leveling;

public class LevelingModule
{
    private readonly DatabaseService _database;

    public LevelingModule(DiscordSocketClient client, DatabaseService database)
    {
        _database = database;
        client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        client.MessageReceived += OnMessageReceivedAsync;
        Log.Logger.Information("Leveling Module Loaded");
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        var guild = (message.Channel as SocketGuildChannel)?.Guild;
        if (guild is null)
        {
            return;
        }
        if (message.Author.IsBot || message.Author.IsWebhook)
        {
            return;
        }
        var config = await _database.GetGuildConfigAsync(guild).ConfigureAwait(false);
        if (config is null)
        {
            return;
        }
        if (!config.Leveling.Enabled)
        {
            return;
        }

        var rate = new Random().NextDouble();
        var msgLength = message.Content.Length;
        var pointsToGive = (int)Math.Floor((rate * 100) + (msgLength / 2));
        var user = message.Author;
        await HandleGivePointsAsync(user, guild, config, pointsToGive).ConfigureAwait(false);
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot)
        {
            return;
        }
        var guild = after.VoiceChannel?.Guild ?? before.VoiceChannel?.Guild;
        var config = await _database.GetGuildConfigAsync(guild!).ConfigureAwait(false);
        if (!config.Leveling.Enabled)
        {
            return;
        }
        if (JoinedChannel(before, after))
        {
            if (IsMuted(after) || JoinedOrLeftAfkChannel(after, config) || SwitchedChannel(before, after))
            {
                return;
            }
            await _database.UpdateUserAsync(guild, user, x => x.LastVoiceChannelJoin = DateTime.UtcNow).ConfigureAwait(false);
            Log.Logger.Information("Set Join Date for {0} by JoinedChannel", user.Username);
        }
        else if (LeftChannel(after))
        {
            if (IsMuted(before) || JoinedOrLeftAfkChannel(before, config) || SwitchedChannel(before, after))
            {
                return;
            }
            var joinDate = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).LastVoiceChannelJoin;
            await HandleGivePointsAsync(user, guild, config, (int)(DateTime.UtcNow - joinDate).TotalSeconds).ConfigureAwait(false);
            Log.Logger.Information("Gave points to {0} by LeftChannel", user.Username);
        }
        else if (Muted(before, after))
        {
            if (JoinedOrLeftAfkChannel(after, config))
            {
                return;
            }
            var joinDate = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).LastVoiceChannelJoin;
            await HandleGivePointsAsync(user, guild, config, (int)(DateTime.UtcNow - joinDate).TotalSeconds).ConfigureAwait(false);
            Log.Logger.Information("Gave points to {0} by Muted", user.Username);
        }
        else if (UnMuted(before, after))
        {
            if (JoinedOrLeftAfkChannel(after, config))
            {
                return;
            }
            await _database.UpdateUserAsync(guild, user, x => x.LastVoiceChannelJoin = DateTime.UtcNow).ConfigureAwait(false);
            Log.Logger.Information("Set Join Date {0} by Unmuted", user.Username);
        }
        else if (SwitchedChannelFromAfk(before, after, config))
        {
            await _database.UpdateUserAsync(guild, user, x => x.LastVoiceChannelJoin = DateTime.UtcNow).ConfigureAwait(false);
            Log.Logger.Information("Set Join Date for {0} by SwitchedChannelFromAfk", user.Username);
        }
        else if (JoinedOrLeftAfkChannel(after, config))
        {
            var joinDate = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).LastVoiceChannelJoin;
            await HandleGivePointsAsync(user, guild, config, (int)(DateTime.UtcNow - joinDate).TotalSeconds).ConfigureAwait(false);
            Log.Logger.Information("Gave points to {0} by JoinedOrLeftAfkChannel", user.Username);
        }
    }

    private async Task HandleGivePointsAsync(SocketUser user, SocketGuild guild, GuildConfig config, int points)
    {
        var currentLevel = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).Level;
        var currentRequiredPoints = Math.Pow(currentLevel * 4, 2);
        var dbUser = await _database.UpdateUserAsync(guild, user, x => x.XP += points).ConfigureAwait(false);
        if (dbUser.XP >= currentRequiredPoints)
        {
            await HandleLevelUpAsync(guild, guild.GetUser(user.Id), config, dbUser.XP, currentLevel).ConfigureAwait(false);
        }
    }

    private static bool SwitchedChannel(SocketVoiceState before, SocketVoiceState after)
    {
        return before.VoiceChannel is not null && after.VoiceChannel is not null && before.VoiceChannel != after.VoiceChannel;
    }

    private static bool SwitchedChannelFromAfk(SocketVoiceState before, SocketVoiceState after, GuildConfig config)
    {
        return before.VoiceChannel is not null && after.VoiceChannel is not null && before.VoiceChannel.Id == config.Leveling.AfkChannelId;
    }

    private static bool JoinedOrLeftAfkChannel(SocketVoiceState voiceState, GuildConfig config)
    {
        return voiceState.VoiceChannel?.Id == config.Leveling.AfkChannelId;
    }

    private static bool LeftChannel(SocketVoiceState voiceState)
    {
        return voiceState.VoiceChannel is null;
    }

    private static bool JoinedChannel(SocketVoiceState before, SocketVoiceState after)
    {
        return before.VoiceChannel is null && after.VoiceChannel is not null;
    }

    private static bool Muted(SocketVoiceState before, SocketVoiceState after)
    {
        var isMuted = !before.IsMuted && after.IsMuted;
        var isSelfMuted = !before.IsSelfMuted && after.IsSelfMuted;
        return isMuted || isSelfMuted;
    }

    private static bool UnMuted(SocketVoiceState before, SocketVoiceState after)
    {
        var isMuted = before.IsMuted && !after.IsMuted;
        var isSelfMuted = before.IsSelfMuted && !after.IsSelfMuted;
        return isMuted || isSelfMuted;
    }
    private static bool IsMuted(SocketVoiceState voiceState)
    {
        return voiceState.IsMuted || voiceState.IsSelfMuted;
    }

    private async Task HandleLevelUpAsync(SocketGuild guild, SocketGuildUser user, GuildConfig config, int xp, int level)
    {
        var xpToLevelUp = (int)Math.Pow(level * 4, 2);
        var levelsToAdd = 0;
        var newPoints = 0;
        switch (xp % xpToLevelUp)
        {
            case 0:
            {
                levelsToAdd = xp / xpToLevelUp;
                newPoints = 0;
                break;
            }
            case > 0:
            {
                levelsToAdd = xp / xpToLevelUp;
                var total = 0;
                for (var i = level; i < level + levelsToAdd; i++)
                {
                    total += (int)Math.Pow(i * 4, 2);
                }
                newPoints = xp - total;
                break;
            }
        }
        var dbUser = await _database.UpdateUserAsync(guild, user, x =>
        {
            x.Level += levelsToAdd;
            x.XP = newPoints;
        }).ConfigureAwait(false);

        var levelRoles = config.Leveling.LevelRoles.FindAll(x => x.Level <= dbUser.Level);

        if (levelRoles.Count > 0)
        {
            var roles = levelRoles.OrderByDescending(x => x.Level).ToList();
            var highestRole = roles[0];
            if (!user.Roles.Any(x => x.Id == highestRole.RoleId))
            {
                var role = guild.GetRole(highestRole.RoleId);
                await user.AddRoleAsync(role).ConfigureAwait(false);
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = guild.Name,
                        IconUrl = guild.IconUrl,
                    },
                    Title = "Új jutalmat szereztél!",
                    Description = $"`{role.Name}`",
                    Color = Color.Gold,
                }.Build();
                var dmchannel = await user.CreateDMChannelAsync().ConfigureAwait(false);
                await dmchannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }

            foreach (var roleToRemove in roles.Skip(1).Select(x => guild.GetRole(x.RoleId)).Where(x => user.Roles.Contains(x)))
            {
                await user.RemoveRoleAsync(roleToRemove).ConfigureAwait(false);
            }
        }

        if (guild.GetChannel(config.Leveling.AnnouncementChannelId) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{dbUser.Level}** szintet!").ConfigureAwait(false);
        }
    }
}