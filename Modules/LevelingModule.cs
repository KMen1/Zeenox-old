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
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService Database;
    private readonly List<(SocketUser user, DateTime startTime)> levels = new();
    private readonly int PointsToLevelUp;
    private readonly ulong LevelUpChannel;

    public LevelingModule(DiscordSocketClient client, ConfigModel.Config config, DatabaseService database)
    {
        _client = client;
        Database = database;
        PointsToLevelUp = config.Leveling.PointsToLevelUp;
        LevelUpChannel = config.Leveling.LevelUpAnnouncementChannelId;
    }
    
    public void Initialize()
    {
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        _client.MessageReceived += OnMessageReceivedAsync;
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

        var points = await Database.AddPointsByUserId(((ITextChannel) arg.Channel).GuildId, user.Id, pointsToGive);
        if (points >= PointsToLevelUp)
        {
            await HandleLevelUp(guild, user, points);
            return;
        }
        var level = await Database.GetUserLevelById(((ITextChannel) arg.Channel).GuildId, user.Id);
        if (level is not (10 or 25 or 50 or 75 or 100))
        {
            return;
        }
        var userRoles = guild.GetUser(user.Id).Roles;
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (!userRoles.Contains(role))
        {
            await HandleLevelUpBySetLevel(user, guild);
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
            var points = await Database.AddPointsByUserId(before.VoiceChannel.Guild.Id, user.Id, pointsToGive);
            levels.Remove(userToGivePointTo);
            if (points >= PointsToLevelUp)
            {
                await HandleLevelUp(guild, user, points);
            }
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

        var level = await Database.AddLevelByUserId(guild.Id, user.Id, levelsToAdd);
        await Database.SetPointsByUserId(guild.Id, user.Id, newPoints);
            
        // give the user a corresponding role when reaching level 10, 25, 50, 75, 100
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role != null)
        {
            await guild.GetUser(user.Id).AddRoleAsync(role);
            var roles = guild.GetUser(user.Id).Roles;
            var lowerLevelRoles = roles.Where(x => x.Name.Contains("Lvl.") && x.Name != role.Name);
            foreach (var lowerLevelRole in lowerLevelRoles)
            {
                await guild.GetUser(user.Id).RemoveRoleAsync(lowerLevelRole);
            }

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = guild.Name,
                    IconUrl = guild.IconUrl,
                },
                Title = $"Új jutalmat szereztél!",
                Description = $"`{role.Name}`",
                Color = Color.Gold,
            }.Build();
            var dmchannel = await user.CreateDMChannelAsync();
            await dmchannel.SendMessageAsync(embed: embed);
        }

        if (guild.GetChannel(LevelUpChannel) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{level}** szintet!");
        }
    }
    
    private async Task HandleLevelUpBySetLevel(SocketUser user, SocketGuild guild)
    {
        var level = await Database.GetUserLevelById(guild.Id, user.Id);
        var role = guild?.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role != null && !guild.GetUser(user.Id).Roles.Contains(role))
        {
            await guild.GetUser(user.Id).AddRoleAsync(role);
            var roles = guild.GetUser(user.Id).Roles;
            var lowerLevelRoles = roles.Where(x => x.Name.Contains("Lvl.") && x.Name != role.Name);
            var usertoremoveform = guild.GetUser(user.Id);
            foreach (var lowerLevelRole in lowerLevelRoles)
            {
                await usertoremoveform.RemoveRoleAsync(lowerLevelRole);
            }
            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = guild.Name,
                    IconUrl = guild.IconUrl,
                },
                Title = $"Új jutalmat szereztél!",
                Description = $"`{role.Name}`",
                Color = Color.Gold,
            }.Build();
            var dmchannel = await user.CreateDMChannelAsync();
            await dmchannel.SendMessageAsync(embed: embed);
        }
        
        if (guild.GetChannel(LevelUpChannel) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{level}** szintet!");
        }
    }
}