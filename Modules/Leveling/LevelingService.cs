using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Config;
using KBot.Database;

namespace KBot.Modules.Leveling;

public class LevelingModule
{
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _database;
    private readonly ulong _levelUpChannel;
    private readonly int _pointsToLevelUp;

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
        var pointsToGive = (int)Math.Floor((rate * 100) + msgLength / 2);
        var user = arg.Author;

        var points = await _database.AddPointsByUserIdAsync(((ITextChannel)arg.Channel).GuildId, user.Id, pointsToGive).ConfigureAwait(false);
        if (points >= _pointsToLevelUp)
        {
            await HandleLevelUpAsync(guild, user, points).ConfigureAwait(false);
            return;
        }
        var level = await _database.GetUserLevelByIdAsync(((ITextChannel)arg.Channel).GuildId, user.Id).ConfigureAwait(false);
        var userRoles = guild.GetUser(user.Id).Roles;
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role is not null && !userRoles.Contains(role))
        {
            await HandleLevelUpBySetLevelAsync(user, guild).ConfigureAwait(false);
        }
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot)
        {
            return;
        }

        var guild = after.VoiceChannel?.Guild ?? before.VoiceChannel?.Guild;
        if (guild is null)
        {
            return;
        }

        if (before.VoiceChannel is null)
        {
            await _database.SetUserVoiceChannelJoinDateByIdAsync(guild.Id, user.Id, DateTime.Now).ConfigureAwait(false);
        }
        else if (after.VoiceChannel is null)
        {
            var joinDate = await _database.GetUserVoiceChannelJoinDateByIdAsync(guild.Id, user.Id).ConfigureAwait(false);
            var pointsToGive = (int) (DateTime.UtcNow - joinDate).TotalSeconds;
            var points = await _database.AddPointsByUserIdAsync(guild.Id, user.Id, pointsToGive).ConfigureAwait(false);
            if (points >= _pointsToLevelUp)
            {
                await HandleLevelUpAsync(guild, user, points).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleLevelUpAsync(SocketGuild guild, IUser user, int points)
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
                newPoints = points - (levelsToAdd * _pointsToLevelUp);
                break;
            }
        }

        var level = await _database.AddLevelByUserIdAsync(guild.Id, user.Id, levelsToAdd).ConfigureAwait(false);
        await _database.SetPointsByUserIdAsync(guild.Id, user.Id, newPoints).ConfigureAwait(false);

        // give the user a corresponding role when reaching level 10, 25, 50, 75, 100
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role is not null)
        {
            await guild.GetUser(user.Id).AddRoleAsync(role).ConfigureAwait(false);
            var roles = guild.GetUser(user.Id).Roles;
            var lowerLevelRoles = roles.Where(x => x.Name.Contains("Lvl.") && x.Name != role.Name);
            foreach (var lowerLevelRole in lowerLevelRoles)
            {
                await guild.GetUser(user.Id).RemoveRoleAsync(lowerLevelRole).ConfigureAwait(false);
            }

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

        if (guild.GetChannel(_levelUpChannel) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{level}** szintet!").ConfigureAwait(false);
        }
    }

    private async Task HandleLevelUpBySetLevelAsync(IUser user, SocketGuild guild)
    {
        var level = await _database.GetUserLevelByIdAsync(guild.Id, user.Id).ConfigureAwait(false);
        var role = guild.Roles.FirstOrDefault(x => x.Name.Contains($"(Lvl. {level})"));
        if (role is not null && !guild.GetUser(user.Id).Roles.Contains(role))
        {
            await guild.GetUser(user.Id).AddRoleAsync(role).ConfigureAwait(false);
            var roles = guild.GetUser(user.Id).Roles;
            var lowerLevelRoles = roles.Where(x => x.Name.Contains("Lvl.") && x.Name != role.Name);
            var usertoremoveform = guild.GetUser(user.Id);
            foreach (var lowerLevelRole in lowerLevelRoles)
            {
                await usertoremoveform.RemoveRoleAsync(lowerLevelRole).ConfigureAwait(false);
            }
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

        if (guild.GetChannel(_levelUpChannel) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync($"🥳 Gratulálok {user.Mention}, elérted a **{level}** szintet!").ConfigureAwait(false);
        }
    }
}