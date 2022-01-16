using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;
using KBot.Database;

namespace KBot.Modules;

public class LevelingModule
{
    private readonly DatabaseService Database;
    private readonly List<(SocketUser user, DateTime startTime)> levels = new();
    private readonly int PointsToLevelUp;
    private readonly DiscordSocketClient _client;
    
    public LevelingModule(DiscordSocketClient client, ConfigModel.Config config, DatabaseService database)
    {
        _client = client;
        Database = database;
        PointsToLevelUp = config.Leveling.PointsToLevelUp;
    }
    
    public Task InitializeAsync()
    {
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        _client.MessageReceived += OnMessageReceivedAsync;
        return Task.CompletedTask;
    } 

    private async Task OnMessageReceivedAsync(SocketMessage arg)
    {
        var guild = (arg.Channel as SocketGuildChannel)?.Guild;
        if (guild == null)
        {
            return;
        }
        if (arg.Author.IsBot || arg.Author.IsWebhook)
        {
            return;
        }
        //calculate random xp based on message length
        var rate = new Random().NextDouble();
        var msgLength = arg.Content.Length;
        var pointsToGive = (int)Math.Floor(rate * 100 + msgLength / 2);
        var user = arg.Author;

        var points = await Database.AddPointsByUserId(user.Id, pointsToGive);
        if (points >= PointsToLevelUp)
        {
            await HandleLevelUp(guild, user, points);
        }
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        var guild = after.VoiceChannel?.Guild ?? before.VoiceChannel?.Guild;
        
        if (!user.IsBot && before.VoiceChannel == null)
        {
            levels.Add((user, DateTime.UtcNow));
        }
        else if (!user.IsBot && after.VoiceChannel == null)
        {
            var userToGivePointTo = levels.FirstOrDefault(x => x.user.Id == user.Id);
            var pointsToGive = (int) (DateTime.UtcNow - userToGivePointTo.startTime).TotalSeconds;
            var points = await Database.AddPointsByUserId(user.Id, pointsToGive);
            if (points >= PointsToLevelUp)
            {
                await HandleLevelUp(guild, user, points);
            }
            levels.Remove(userToGivePointTo);
        }
    }

    private async Task HandleLevelUp(SocketGuild guild, IUser user, int points)
    {
        var levelsToAdd = 0;
        var newPoints = 0;
        if (points % PointsToLevelUp == 0)
        {
            levelsToAdd = points / PointsToLevelUp;
            newPoints = 0;
        }
        else if (points % PointsToLevelUp > 0)
        {
            levelsToAdd = points / PointsToLevelUp;
            newPoints = points - levelsToAdd * PointsToLevelUp;
        }

        var level = await Database.AddLevelByUserId(user.Id, levelsToAdd);
        await Database.SetPointsByUserId(user.Id, newPoints);
            
        // give the user a corresponding role when reaching level 10, 25, 50, 75, 100
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"Lvl. {level}"));
        if (role != null)
        {
            await guild.GetUser(user.Id).AddRoleAsync(role);
        }
            
        var channel = await user.CreateDMChannelAsync();
        await channel.SendMessageAsync($"🥳 Gratulálok, elérted a {level} szintet!");
    }
}