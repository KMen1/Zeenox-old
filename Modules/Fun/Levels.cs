using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Database;

namespace KBot.Modules.Fun;

public class Levels : KBotModuleBase
{
    public DatabaseService Database { get; set; }
    
    [SlashCommand("level", "Saját szint és xp lekérése")]
    public async Task GetLevel(SocketUser user = null)
    {
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
}