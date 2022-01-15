using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Database;
using KBot.Enums;

namespace KBot.Modules.Moderation;

public class WarnModule : KBotModuleBase
{
    public DatabaseService Database { get; set; }
    
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("warn", "Figyelmeztetést ad az adott felhasználónak.")]
    public async Task WarnAsync(SocketUser user, string reason)
    {
        var moderatorId = Context.User.Id;
        var userId = user.Id;
        await DeferAsync();
        await Database.AddWarnToUser(userId, moderatorId, reason);
        await FollowupWithEmbedAsync(EmbedResult.Success, $"{user.Username} sikeresen figyelmeztetve!", $"A következő indokkal: `{reason}`");
    }
    
    [SlashCommand("warns", "A felhasználó figyelmeztetéseinek listája.")]
    public async Task WarnsAsync(SocketUser user)
    {
        var userId = user.Id;
        await DeferAsync();
        var warns = await Database.GetWarnsByUserId(userId);
        if (warns is null)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "😎 Szép munka!", $"{user.Mention} még nem rendelkezik figyelmeztetéssel. Maradjon is így!");
            return;
        }
        
        var warnString = new StringBuilder();
        foreach (var warn in warns)
        {
            warnString.AppendLine($"{warns.TakeWhile(n => n != warn).Count() + 1}. {Context.Client.GetUser(warn.ModeratorId).Mention} által - Indok:`{warn.Reason}`");
        }
        await FollowupWithEmbedAsync(EmbedResult.Success, $"{user.Username} figyelmeztetései sikeresen lekérve", warnString.ToString());
    }
    
}