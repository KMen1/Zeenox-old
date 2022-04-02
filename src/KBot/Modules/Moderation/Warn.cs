using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Models;

namespace KBot.Modules.Moderation;

[Group("mod", "Moderációs parancsok")]
public class WarnModule : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("warn", "Figyelmeztetést ad az adott felhasználónak.")]
    public async Task WarnAsync(SocketUser user, string reason)
    {
        var moderatorId = Context.User.Id;
        await DeferAsync(true).ConfigureAwait(false);
        if (Context.Guild.GetUser(user.Id).GuildPermissions.KickMembers)
        {
            await FollowupWithEmbedAsync(Color.Red,"Sikertelen figyelmeztetés", "Más moderátort nem tudsz figyelmeztetni").ConfigureAwait(false);
            return;
        }
        await Database.UpdateUserAsync(Context.Guild, user, x => x.Warns.Add(new Warn(moderatorId, reason, DateTime.UtcNow))).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Orange, $"{user.Username} sikeresen figyelmeztetve!",
            $"A következő indokkal: `{reason}`").ConfigureAwait(false);

        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle($"Figyelmeztetve lettél {Context.Guild.Name}-ban!")
            .WithColor(Color.Red)
            .WithDescription($"{Context.User.Mention} moderátor által \n A következő indokkal: `{reason}`")
            .WithCurrentTimestamp()
            .Build();
        try
        {
            await channel.SendMessageAsync(embed:eb).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [SlashCommand("unwarn", "Figyelmeztetést ad az adott felhasználónak.")]
    public async Task RemoveWarnAsync(SocketUser user, string reason, [MinValue(1)]int warnId)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var dbUser = await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false);
        if (warnId > dbUser.Warns.Count)
        {
            await FollowupWithEmbedAsync(Color.Red,"Hiba", "Nincs ilyen figyelmeztetés!").ConfigureAwait(false);
            return;
        }
        await Database.UpdateUserAsync(Context.Guild, user, x => x.Warns.RemoveAt(warnId - 1)).ConfigureAwait(false);
        await FollowupWithEmbedAsync(Color.Green,
                $"{user.Username} {warnId} számú figyelmeztetése eltávolítva!", $"A következő indokkal: `{reason}`")
            .ConfigureAwait(false);
        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        var eb = new EmbedBuilder()
            .WithTitle($"Eltávolítottak rólad egy figyelmeztetést {Context.Guild.Name}-ban!")
            .WithColor(Color.Green)
            .WithDescription($"{Context.User.Mention} moderátor által \n A következő indokkal: `{reason}`")
            .WithCurrentTimestamp()
            .Build();
        try
        {
            await channel.SendMessageAsync(embed:eb).ConfigureAwait(false);
        }
        catch
        {
            // ignored
        }
    }

    [SlashCommand("warns", "A felhasználó figyelmeztetéseinek listája.")]
    public async Task WarnsAsync(SocketUser user)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var warns = (await Database.GetUserAsync(Context.Guild, user).ConfigureAwait(false)).Warns;
        if (warns.Count == 0)
        {
            await FollowupWithEmbedAsync(Color.Gold, "😎 Szép munka!",
                $"{user.Mention} még nem rendelkezik figyelmeztetéssel. Maradjon is így!").ConfigureAwait(false);
            return;
        }

        var warnString = new StringBuilder();
        foreach (var warn in warns)
        {
            warnString.AppendLine(
                $"{warns.TakeWhile(n => n != warn).Count() + 1}. {Context.Client.GetUser(warn.ModeratorId).Mention} által - Indok:`{warn.Reason}`");
        }
        await FollowupWithEmbedAsync(Color.Orange, $"{user.Username} figyelmeztetései", warnString.ToString(), ephemeral: true).ConfigureAwait(false);
    }
}