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
        await GivePointsAsync(user, guild, config, pointsToGive).ConfigureAwait(false);
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
            if (IsMuted(after) || IsInAfkChannel(after, config) || SwitchedChannel(before, after))
            {
                return;
            }
            await _database.UpdateUserAsync(guild, user, x => x.LastVoiceActivityDate = DateTime.UtcNow).ConfigureAwait(false);
            Log.Logger.Information("Set Join Date for {0} by JoinedChannel", user.Username);
        }
        else if (LeftChannel(after))
        {
            if (IsMuted(before) || IsInAfkChannel(before, config) || SwitchedChannel(before, after))
            {
                return;
            }
            var joinDate = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).LastVoiceActivityDate;
            await GivePointsAsync(user, guild, config, (int)(DateTime.UtcNow - joinDate).TotalSeconds).ConfigureAwait(false);
            Log.Logger.Information("Gave points to {0} by LeftChannel", user.Username);
        }
        else if (Muted(before, after))
        {
            if (IsInAfkChannel(after, config))
            {
                return;
            }
            var joinDate = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).LastVoiceActivityDate;
            await GivePointsAsync(user, guild, config, (int)(DateTime.UtcNow - joinDate).TotalSeconds).ConfigureAwait(false);
            Log.Logger.Information("Gave points to {0} by Muted", user.Username);
        }
        else if (UnMuted(before, after))
        {
            if (IsInAfkChannel(after, config))
            {
                return;
            }
            await _database.UpdateUserAsync(guild, user, x => x.LastVoiceActivityDate = DateTime.UtcNow).ConfigureAwait(false);
            Log.Logger.Information("Set Join Date {0} by Unmuted", user.Username);
        }
        else if (SwitchedChannelFromAfk(before, after, config.Leveling.AfkChannelId))
        {
            await _database.UpdateUserAsync(guild, user, x => x.LastVoiceActivityDate = DateTime.UtcNow).ConfigureAwait(false);
            Log.Logger.Information("Set Join Date for {0} by SwitchedChannelFromAfk", user.Username);
        }
        else if (IsInAfkChannel(after, config))
        {
            var joinDate = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).LastVoiceActivityDate;
            await GivePointsAsync(user, guild, config, (int)(DateTime.UtcNow - joinDate).TotalSeconds).ConfigureAwait(false);
            Log.Logger.Information("Gave points to {0} by JoinedAfkChannel", user.Username);
        }
    }

    private async Task GivePointsAsync(SocketUser user, SocketGuild guild, GuildConfig config, int points)
    {
        var currentLevel = (await _database.GetUserAsync(guild, user).ConfigureAwait(false)).Level;
        var currentRequiredPoints = Math.Pow(currentLevel * 4, 2);
        var dbUser = await _database.UpdateUserAsync(guild, user, x => x.XP += points).ConfigureAwait(false);
        if (dbUser.XP >= currentRequiredPoints)
        {
            await HandleLevelUpAsync(guild.GetUser(user.Id), config, dbUser.XP, currentLevel).ConfigureAwait(false);
        }
    }

    private static bool SwitchedChannel(SocketVoiceState before, SocketVoiceState after)
    {
        return before.VoiceChannel is not null && after.VoiceChannel is not null && before.VoiceChannel != after.VoiceChannel;
    }

    private static bool SwitchedChannelFromAfk(SocketVoiceState before, SocketVoiceState after, ulong afkChannelId)
    {
        return before.VoiceChannel is not null && after.VoiceChannel is not null && before.VoiceChannel.Id == afkChannelId;
    }

    private static bool IsInAfkChannel(SocketVoiceState voiceState, GuildConfig config)
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
        return !IsMuted(before) || IsMuted(after);
    }

    private static bool UnMuted(SocketVoiceState before, SocketVoiceState after)
    {
        return IsMuted(before) || !IsMuted(after);
    }
    private static bool IsMuted(SocketVoiceState voiceState)
    {
        return voiceState.IsMuted || voiceState.IsSelfMuted;
    }

    private async Task HandleLevelUpAsync(SocketGuildUser user, GuildConfig config, int xp, int level)
    {
        var guild = user.Guild;
        var xpToLevelUp = (int)Math.Pow(level * 4, 2);
        var dbUser = await _database.UpdateUserAsync(guild, user, x =>
        {
            switch (xp % xpToLevelUp)
            {
                case 0:
                {
                    x.Level += xp / xpToLevelUp;
                    x.XP = 0;
                    break;
                }
                case > 0:
                {
                    x.Level += xp / xpToLevelUp;
                    var total = 0;
                    for (var i = level; i < level + xp / xpToLevelUp; i++)
                    {
                        total += (int)Math.Pow(i * 4, 2);
                    }
                    x.XP = xp - total;
                    break;
                }
            }
        }).ConfigureAwait(false);

        var lowerLevelRoles = config.Leveling.LevelRoles.FindAll(x => x.Level <= dbUser.Level);

        if (lowerLevelRoles.Count > 0)
        {
            var roles = lowerLevelRoles.OrderByDescending(x => x.Level).ToList();
            var highestRole = roles[0];
            if (user.Roles.All(x => x.Id != highestRole.Id))
            {
                var role = guild.GetRole(highestRole.Id);
                await user.AddRoleAsync(role).ConfigureAwait(false);
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = guild.Name,
                        IconUrl = guild.IconUrl,
                    },
                    Title = "Új jutalmat szereztél!",
                    Description = $"`{role.Mention}`",
                    Color = Color.Gold,
                }.Build();
                var dmchannel = await user.CreateDMChannelAsync().ConfigureAwait(false);
                await dmchannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }

            foreach (var roleToRemove in roles.Skip(1).Select(x => guild.GetRole(x.Id)).Where(x => user.Roles.Contains(x)))
            {
                await user.RemoveRoleAsync(roleToRemove).ConfigureAwait(false);
            }
        }

        if (guild.GetChannel(config.Leveling.AnnounceChannelId) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{dbUser.Level}** szintet!").ConfigureAwait(false);
        }
    }
}