using System;
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
    private readonly DatabaseService _database;
    //private readonly List<(SocketUser user, DateTime startTime)> levels = new();
    private readonly int _pointsToLevelUp;
    private readonly ulong _levelUpChannel;

    public LevelingModule(DiscordSocketClient client, ConfigModel.Config config, DatabaseService database)
    {
        _client = client;
        _database = database;
        _pointsToLevelUp = config.Leveling.PointsToLevelUp;
        _levelUpChannel = config.Leveling.LevelUpAnnouncementChannelId;
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

        var points = await _database.AddPointsByUserId(((ITextChannel) arg.Channel).GuildId, user.Id, pointsToGive);
        if (points >= _pointsToLevelUp)
        {
            await HandleLevelUp(guild, user, points);
            return;
        }
        var level = await _database.GetUserLevelById(((ITextChannel) arg.Channel).GuildId, user.Id);
        var userRoles = guild.GetUser(user.Id).Roles;
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role is not null && !userRoles.Contains(role))
        {
            await HandleLevelUpBySetLevel(user, guild);
        }
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot)
        {
            return;
        }
        var guild = after.VoiceChannel?.Guild ?? before.VoiceChannel?.Guild;
        
        if (before.VoiceChannel is null)
        {
            await _database.SetUserVoiceChannelJoinDateById(guild.Id, user.Id, DateTime.Now);
            //levels.Add((user, DateTime.UtcNow));
        }
        else if (after.VoiceChannel is null)
        {
            var joinDate = await _database.GetUserVoiceChannelJoinDateById(guild.Id, user.Id);
            //var userToGivePointTo = levels.FirstOrDefault(x => x.user.Id == user.Id);
            
            var pointsToGive = (int) (DateTime.UtcNow - joinDate).TotalSeconds;
            var points = await _database.AddPointsByUserId(guild.Id, user.Id, pointsToGive);
            //levels.Remove(userToGivePointTo);
            if (points >= _pointsToLevelUp)
            {
                await HandleLevelUp(guild, user, points);
            }
        }
    }

    private async Task HandleLevelUp(SocketGuild guild, IUser user, int points)
    {
        var levelsToAdd = 0;
        var newPoints = 0;
        switch (points % _pointsToLevelUp)
        {
            case 0:
            {
                levelsToAdd = points / _pointsToLevelUp;
                newPoints = 0;
                break;
            }
            case > 0:
            {
                levelsToAdd = points / _pointsToLevelUp;
                newPoints = points - levelsToAdd * _pointsToLevelUp;
                break;
            }
        }

        var level = await _database.AddLevelByUserId(guild.Id, user.Id, levelsToAdd);
        await _database.SetPointsByUserId(guild.Id, user.Id, newPoints);
            
        // give the user a corresponding role when reaching level 10, 25, 50, 75, 100
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role is not null)
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

        if (guild.GetChannel(_levelUpChannel) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{level}** szintet!");
        }
    }
    
    private async Task HandleLevelUpBySetLevel(IUser user, SocketGuild guild)
    {
        var level = await _database.GetUserLevelById(guild.Id, user.Id);
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role is not null && !guild.GetUser(user.Id).Roles.Contains(role))
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
        
        if (guild.GetChannel(_levelUpChannel) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{level}** szintet!");
        }
    }
}