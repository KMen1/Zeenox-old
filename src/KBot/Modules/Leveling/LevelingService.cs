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

public class LevelingModule : DiscordClientService
{
    private readonly DatabaseService _database;

    public LevelingModule(DiscordSocketClient client, ILogger<LevelingModule> logger, DatabaseService database) : base(client, logger)
    {
        _database = database;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        Client.MessageReceived += OnMessageReceivedAsync;
        Log.Logger.Information("Leveling Module Loaded");
        return Task.CompletedTask;
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
        var config = await _database.GetGuildConfigFromCacheAsync(guild.Id).ConfigureAwait(false);
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
        var points = await _database.AddPointsAsync(guild.Id, user.Id, pointsToGive).ConfigureAwait(false);
        if (points >= config.Leveling.PointsToLevelUp)
        {
            await HandleLevelUpAsync(guild, guild.GetUser(user.Id), points, config).ConfigureAwait(false);
        }
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot)
        {
            return;
        }
        var guild = after.VoiceChannel?.Guild ?? before.VoiceChannel?.Guild;
        var config = await _database.GetGuildConfigFromCacheAsync(guild!.Id).ConfigureAwait(false);
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
            Log.Logger.Information("Set Join Date for {0} by JoinedChannel", user.Username);
            await _database.SetVoiceChannelJoinDateAsync(guild.Id, user.Id, DateTime.UtcNow).ConfigureAwait(false);
        }
        else if (LeftChannel(after))
        {
            if (IsMuted(before) || JoinedOrLeftAfkChannel(before, config) || SwitchedChannel(before, after))
            {
                return;
            }
            Log.Logger.Information("Gave points to {0} by LeftChannel", user.Username);
            await HandleGivePoints(user, guild, config).ConfigureAwait(false);
        }
        else if (Muted(before, after))
        {
            if (JoinedOrLeftAfkChannel(after, config))
            {
                return;
            }
            Log.Logger.Information("{0} got muted", user.Username);
            await HandleGivePoints(user, guild, config).ConfigureAwait(false);
        }
        else if (UnMuted(before, after))
        {
            if (JoinedOrLeftAfkChannel(after, config))
            {
                return;
            }
            Log.Logger.Information("{0} got unmuted", user.Username);
            await _database.SetVoiceChannelJoinDateAsync(guild.Id, user.Id, DateTime.UtcNow).ConfigureAwait(false);
        }
        else if (SwitchedChannelFromAfk(before, after, config))
        {
            Log.Logger.Information("Set Join Date for {0} by SwitchedChannelFromAfk", user.Username);
            await _database.SetVoiceChannelJoinDateAsync(guild.Id, user.Id, DateTime.UtcNow).ConfigureAwait(false);
        }
        else if (JoinedOrLeftAfkChannel(after, config))
        {
            Log.Logger.Information("Gave points to {0} by JoinedOrLeftAfkChannel", user.Username);
            await HandleGivePoints(user, guild, config).ConfigureAwait(false);
        }
    }

    private async Task HandleGivePoints(SocketUser user, SocketGuild guild, GuildConfig config)
    {
        var joinDate = await _database.GetVoiceChannelJoinDateAsync(guild.Id, user.Id).ConfigureAwait(false);
        var pointsToGive = (int) (DateTime.UtcNow - joinDate).TotalSeconds;
        var points = await _database.AddPointsAsync(guild.Id, user.Id, pointsToGive).ConfigureAwait(false);
        if (points >= config.Leveling.PointsToLevelUp)
        {
            await HandleLevelUpAsync(guild, guild.GetUser(user.Id), points, config).ConfigureAwait(false);
        }
    }

    private static bool SwitchedChannel(SocketVoiceState before, SocketVoiceState after)
    {
        return before.VoiceChannel is not null && after.VoiceChannel is not null && before.VoiceChannel != after.VoiceChannel;
    }
    
    private bool SwitchedChannelFromAfk(SocketVoiceState before, SocketVoiceState after, GuildConfig config)
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

    private async Task HandleLevelUpAsync(SocketGuild guild, SocketGuildUser user, int points, GuildConfig config)
    {
        var pointsToLevelUp = config.Leveling.PointsToLevelUp;
        var levelsToAdd = 0;
        var newPoints = 0;
        switch (points % pointsToLevelUp)
        {
            case 0:
            {
                levelsToAdd = points / pointsToLevelUp;
                newPoints = 0;
                break;
            }
            case > 0:
            {
                levelsToAdd = points / pointsToLevelUp;
                newPoints = points - (levelsToAdd * pointsToLevelUp);
                break;
            }
        }
        var newLevel = await _database.AddLevelAsync(guild.Id, user.Id, levelsToAdd).ConfigureAwait(false);
        await _database.SetPointsAsync(guild.Id, user.Id, newPoints).ConfigureAwait(false);

        var levelRoles = config.Leveling.LevelRoles.FindAll(x => x.Level <= newLevel);

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
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{newLevel}** szintet!").ConfigureAwait(false);
        }
    }
}