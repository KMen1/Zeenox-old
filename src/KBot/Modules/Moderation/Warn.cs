using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;

namespace KBot.Modules.Moderation;

[Group("mod", "Moderációs parancsok")]
public class WarnModule : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("warn", "Figyelmeztetést ad az adott felhasználónak.")]
    public async Task WarnAsync(SocketUser user, string reason)
    {
        var moderatorId = Context.User.Id;
        var userId = user.Id;
        await DeferAsync().ConfigureAwait(false);
        await Database.AddWarnAsync(Context.Guild.Id, userId, moderatorId, reason).ConfigureAwait(false);
        await FollowupWithEmbedAsync(EmbedResult.Success, $"{user.Username} sikeresen figyelmeztetve!",
            $"A következő indokkal: `{reason}`").ConfigureAwait(false);

        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle($"Figyelmeztetve lettél {Context.Guild.Name}-ban!")
            .WithColor(Color.Red)
            .WithDescription($"{Context.User.Mention} moderátor által \n A következő indokkal: `{reason}`")
            .WithCurrentTimestamp()
            .Build();
        await channel.SendMessageAsync(embed:eb).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("unwarn", "Figyelmeztetést ad az adott felhasználónak.")]
    public async Task RemoveWarnAsync(SocketUser user, string reason, int warnId)
    {
        await DeferAsync().ConfigureAwait(false);
        var result = await Database.RemoveWarnAsync(Context.Guild.Id, user.Id, warnId).ConfigureAwait(false);
        if (!result)
        {
            await FollowupWithEmbedAsync(EmbedResult.Error, "Nem sikerült a figyelmeztetés törlése!",
                "Ehhez a `warnid`-hez nem tartozik figyelmeztetés!").ConfigureAwait(false);
            return;
        }
        await FollowupWithEmbedAsync(EmbedResult.Success,
                $"{user.Username} {warnId} számú figyelmeztetése eltávolítva!", $"A következő indokkal: `{reason}`")
            .ConfigureAwait(false);
        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle($"Eltávolítottak rólad egy figyelmeztetést {Context.Guild.Name}-ban!")
            .WithColor(Color.Green)
            .WithDescription($"{Context.User.Mention} moderátor által \n A következő indokkal: `{reason}`")
            .WithCurrentTimestamp()
            .Build();
        await channel.SendMessageAsync(embed:eb).ConfigureAwait(false);
    }

    [SlashCommand("warns", "A felhasználó figyelmeztetéseinek listája.")]
    public async Task WarnsAsync(SocketUser user)
    {
        var userId = user.Id;
        await DeferAsync(true).ConfigureAwait(false);
        var warns = await Database.GetWarnsAsync(Context.Guild.Id, userId).ConfigureAwait(false);
        if (warns.Count is 0)
        {
            await FollowupWithEmbedAsync(EmbedResult.Success, "😎 Szép munka!",
                $"{user.Mention} még nem rendelkezik figyelmeztetéssel. Maradjon is így!").ConfigureAwait(false);
            return;
        }

        var warnString = new StringBuilder();
        foreach (var warn in warns)
        {
            warnString.AppendLine(
                $"{warns.TakeWhile(n => n != warn).Count() + 1}. {Context.Client.GetUser(warn.ModeratorId).Mention} által - Indok:`{warn.Reason}`");
        }
        await FollowupWithEmbedAsync(EmbedResult.Success, $"{user.Username} figyelmeztetései", warnString.ToString(), ephemeral: true).ConfigureAwait(false);
    }
}