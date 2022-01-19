using System;
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
        await Database.AddWarnByUserId(Context.Guild.Id, userId, moderatorId, reason);
        await FollowupWithEmbedAsync(EmbedResult.Success, $"{user.Username} sikeresen figyelmeztetve!", $"A következő indokkal: `{reason}`");
        await user.CreateDMChannelAsync().ContinueWith(async (task) =>
        {
            var eb = new EmbedBuilder()
            {
                Title = $"Figyelmeztetve lettél {Context.Guild.Name}-ban!",
                Color = Color.Red,
                Description = $"{Context.User.Mention} moderátor által \n A következő indokkal: `{reason}`",
                Timestamp = DateTimeOffset.UtcNow,
            }.Build();
            var dmChannel = task.Result;
            await dmChannel.SendMessageAsync(embed: eb);
        });
    }
    
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("unwarn", "Figyelmeztetést ad az adott felhasználónak.")]
    public async Task RemoveWarnAsync(SocketUser user, string reason, int warnId)
    {
        var userId = user.Id;
        await DeferAsync();
        var result = await Database.RemoveWarnByUserId(Context.Guild.Id, userId, warnId);
        if (!result)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Nem sikerült a figyelmeztetés törlése!", "Ehhez a `warnid`-hez nem tartozik figyelmeztetés!");
            return;
        }
        await FollowupWithEmbedAsync(EmbedResult.Success, $"{user.Username} {warnId} számú figyelmeztetése eltávolítva!", $"A következő indokkal: `{reason}`");
        await user.CreateDMChannelAsync().ContinueWith(async (task) =>
        {
            var eb = new EmbedBuilder()
            {
                Title = $"Eltávolítottak rólad egy figyelmeztetést {Context.Guild.Name}-ban!",
                Color = Color.Green,
                Description = $"{Context.User.Mention} moderátor által \n A következő indokkal: `{reason}`",
                Timestamp = DateTimeOffset.UtcNow,
            }.Build();
            var dmChannel = task.Result;
            await dmChannel.SendMessageAsync(embed: eb);
        });
    }
    
    [SlashCommand("warns", "A felhasználó figyelmeztetéseinek listája.")]
    public async Task WarnsAsync(SocketUser user)
    {
        var userId = user.Id;
        await DeferAsync();
        var warns = await Database.GetWarnsByUserId(Context.Guild.Id, userId);
        if (warns.Count is 0)
        {
            await FollowupWithEmbedAsync(EmbedResult.Success, "😎 Szép munka!", $"{user.Mention} még nem rendelkezik figyelmeztetéssel. Maradjon is így!");
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