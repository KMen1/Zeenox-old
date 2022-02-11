using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using KBot.Common;
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
        var pointsToGive = (int)Math.Floor((rate * 100) + msgLength / 2);
        var user = message.Author;
        var points = await _database.AddPointsAsync(guild.Id, user.Id, pointsToGive).ConfigureAwait(false);
        if (points >= config.Leveling.PointsToLevelUp)
        {
            await HandleLevelUpAsync(guild, user, points, config).ConfigureAwait(false);
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
        if (before.VoiceChannel is null || before.VoiceChannel.Id == config.Leveling.AfkChannelId)
        {
            await _database.SetVoiceChannelJoinDateAsync(guild.Id, user.Id, DateTime.Now).ConfigureAwait(false);
        }
        else if (after.VoiceChannel is null || after.VoiceChannel.Id == config.Leveling.AfkChannelId)
        {
            var joinDate = await _database.GetVoiceChannelJoinDateAsync(guild.Id, user.Id).ConfigureAwait(false);
            var pointsToGive = (int) (DateTime.UtcNow - joinDate).TotalSeconds;
            var points = await _database.AddPointsAsync(guild.Id, user.Id, pointsToGive).ConfigureAwait(false);
            if (points >= config.Leveling.PointsToLevelUp)
            {
                await HandleLevelUpAsync(guild, user, points, config).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleLevelUpAsync(SocketGuild guild, IUser user, int points, GuildConfig config)
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

        var roleId = config.Leveling.LevelRoles.First(x => x.Level == newLevel).RoleId;
        var roleToAdd = guild.GetRole(roleId);
        if (roleToAdd is not null)
        {
            await guild.GetUser(user.Id).AddRoleAsync(roleToAdd).ConfigureAwait(false);
            var roles = guild.GetUser(user.Id).Roles;
            var lowerLevelRoleIds = config.Leveling.LevelRoles.Where(x => x.Level < newLevel).ToList();
            foreach (var roleToRemove in lowerLevelRoleIds.Select(lowerLevelRole => guild.GetRole(lowerLevelRole.RoleId)).Where(roleToRemove => roles.Contains(roleToRemove)))
            {
                await guild.GetUser(user.Id).RemoveRoleAsync(roleToRemove).ConfigureAwait(false);
            }

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = guild.Name,
                    IconUrl = guild.IconUrl,
                },
                Title = "Új jutalmat szereztél!",
                Description = $"`{roleToAdd.Name}`",
                Color = Color.Gold,
            }.Build();
            var dmchannel = await user.CreateDMChannelAsync().ConfigureAwait(false);
            await dmchannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        if (guild.GetChannel(config.Leveling.AnnouncementChannelId) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{newLevel}** szintet!").ConfigureAwait(false);
        }
    }
}