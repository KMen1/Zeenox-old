using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Services;
using Serilog;
using StackExchange.Redis;

namespace KBot.Modules.Leveling;

public class LevelingService : IInjectable
{
    private readonly MongoService _mongo;
    private readonly IConnectionMultiplexer _redis;
    private readonly ConcurrentQueue<(SocketGuildUser, int)> _xpQueue = new();

    public LevelingService(DiscordSocketClient client, MongoService database, IConnectionMultiplexer redis)
    {
        _mongo = database;
        _redis = redis;
        client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        client.MessageReceived += OnMessageReceivedAsync;
        Task.Run(CheckForLevelUpAsync);
    }

    private async Task CheckForLevelUpAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            try
            {
                var usersToUpdate = new List<(SocketGuildUser, int)>();
                while (_xpQueue.TryDequeue(out var user)) usersToUpdate.Add(user);

                if (usersToUpdate.Count == 0)
                    continue;

                var toNotify = new List<(SocketGuildUser, int, ulong)>();
                foreach (var (user, xp) in usersToUpdate)
                {
                    var config = await _mongo.GetGuildConfigAsync(user.Guild).ConfigureAwait(false);
                    var oldUserData = await _mongo.GetUserAsync(user).ConfigureAwait(false);
                    var newUserData = await _mongo.UpdateUserAsync(user, x =>
                    {
                        x.Xp += xp;
                        if (x.Xp < x.RequiredXp) return;
                        x.Xp -= x.RequiredXp;
                        x.Level++;
                    }).ConfigureAwait(false);

                    if (newUserData.Level == oldUserData.Level)
                        continue;
                    toNotify.Add((user, newUserData.Level, config.LevelUpChannelId));

                    var lowerLevelRoles = config.LevelRoles.FindAll(x => x.Level <= newUserData.Level);
                    if (lowerLevelRoles.Count == 0) continue;

                    var roles = lowerLevelRoles.OrderByDescending(x => x.Level).ToList();
                    var highestRole = roles[0];

                    if (user.Roles.All(x => x.Id != highestRole.Id))
                    {
                        var role = user.Guild.GetRole(highestRole.Id);
                        await user.AddRoleAsync(role).ConfigureAwait(false);
                    }

                    foreach (var roleToRemove in roles.Skip(1).Select(x => user.Guild.GetRole(x.Id))
                                 .Where(x => user.Roles.Contains(x)))
                        await user.RemoveRoleAsync(roleToRemove).ConfigureAwait(false);
                }

                if (toNotify.Count == 0)
                    continue;

                foreach (var (user, level, channelId) in toNotify)
                {
                    if (user.Guild.GetTextChannel(channelId) is not { } channel) continue;
                    var eb = new EmbedBuilder()
                        .WithAuthor($"{user.Username}#{user.Discriminator}", user.GetAvatarUrl())
                        .WithColor(Color.Gold)
                        .WithDescription($"**🎉 Congrats {user.Mention}, you reached level {level}! 🎉**")
                        .Build();
                    await channel.SendMessageAsync(embed: eb)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Error in leveling loop");
            }
        }
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author is not SocketGuildUser user || user.IsBot || user.IsWebhook)
            return;

        var config = await _mongo.GetGuildConfigAsync(user.Guild).ConfigureAwait(false);
        if (config.LevelUpChannelId == 0) return;

        _ = Task.Run(() =>
        {
            if (message.Content.Length < 3)
                return;
            var rate = new Random().NextDouble();
            var msgLength = message.Content.Length;
            var pointsToGive = (int) Math.Floor(rate * msgLength);
            if (pointsToGive > 1000)
                pointsToGive = 1000;
            _xpQueue.Enqueue((user, pointsToGive));
        }).ConfigureAwait(false);
    }

    private Task OnUserVoiceStateUpdatedAsync(SocketUser socketUser, SocketVoiceState before, SocketVoiceState after)
    {
        if (socketUser is not SocketGuildUser user || user.IsBot)
            return Task.CompletedTask;

        _ = Task.Run(() =>
        {
            if (before.VoiceChannel is not null)
                ScanChannelForVoiceXp(before.VoiceChannel);
            if (after.VoiceChannel is not null && after.VoiceChannel != before.VoiceChannel)
                ScanChannelForVoiceXp(after.VoiceChannel);
            else if (after.VoiceChannel is null)
                UserLeftVoiceChannel(user);
        });
        return Task.CompletedTask;
    }

    private void ScanChannelForVoiceXp(SocketVoiceChannel channel)
    {
        if (channel.Users.Where(IsActiveInVoiceChannel).Take(2).Skip(1).Any())
            foreach (var user in channel.Users)
                ScanUserForVoiceXp(user);
        else
            foreach (var user in channel.Users)
                UserLeftVoiceChannel(user);
    }

    private void ScanUserForVoiceXp(SocketGuildUser user)
    {
        if (IsActiveInVoiceChannel(user))
            UserJoinedVoiceChannel(user);
        else
            UserLeftVoiceChannel(user);
    }
    
    private void UserJoinedVoiceChannel(SocketGuildUser user)
    {
        var key = $"{user.Id}_voice_channel_join";
        var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _redis.GetDatabase().StringSet(key, value, TimeSpan.FromHours(24), when: When.NotExists);
        Log.Logger.Information("Set voice channel join key for {user}", user.Username);
    }

    private void UserLeftVoiceChannel(SocketGuildUser user)
    {
        var key = $"{user.Id}_voice_channel_join";
        var value = _redis.GetDatabase().StringGet(key);
        _redis.GetDatabase().KeyDelete(key);

        if (value.IsNull) return;

        if (!value.TryParse(out long startUnixTime))
            return;

        var dateStart = DateTimeOffset.FromUnixTimeSeconds(startUnixTime);
        var dateEnd = DateTimeOffset.UtcNow;
        var xp = (int)(dateEnd - dateStart).TotalSeconds;

        if (xp <= 0) return;
        _xpQueue.Enqueue((user, xp));
        Log.Logger.Information("Queued {xp} xp to {user}", xp, user.Username);
    }
    
    private static bool IsActiveInVoiceChannel(SocketGuildUser user) =>
        !user.IsDeafened && !user.IsMuted && !user.IsSelfDeafened && !user.IsSelfMuted;
}