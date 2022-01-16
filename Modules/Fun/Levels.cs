using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;

namespace KBot.Modules.Fun;

public class Levels : KBotModuleBase
{
    public DatabaseService Database { get; set; }
    
    [SlashCommand("level", "Saját szint és xp lekérése")]
    public async Task GetLevel(SocketUser user = null)
    {
        await DeferAsync();
        var setUser = user ?? Context.User;
        var userId = setUser.Id;
        
        var level = await Database.GetUserLevelById(userId);
        var xp = await Database.GetUserPointsById(userId);

        var embed = new EmbedBuilder()
            .WithAuthor(setUser.Username, setUser.GetAvatarUrl())
            .WithColor(Color.Gold)
            .WithDescription($"**XP: **`{xp}/18000` ({level * 18000 + xp} Összesen) \n**Szint: **`{level}`")
            .Build();

        await FollowupAsync(embed: embed);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("addlevel", "Szint hozzáadása (admin)")]
    public async Task AddLevel(SocketUser user, int levelsToAdd)
    {
        await DeferAsync();
        var level = await Database.AddLevelByUserId(user.Id, levelsToAdd);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!", $"{user.Mention} mostantól {level} szintű!");
    }
    
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("setlevel", "Szint hozzáadása (admin)")]
    public async Task SetLevel(SocketUser user, int level)
    {
        await DeferAsync();
        var newLevel = await Database.SetLevelByUserId(user.Id, level);
        await FollowupWithEmbedAsync(EmbedResult.Success, "Szint hozzáadva!", $"{user.Mention} mostantól {newLevel} szintű!");
    }
}