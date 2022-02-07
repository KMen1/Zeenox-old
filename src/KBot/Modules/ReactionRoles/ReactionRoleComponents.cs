using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Enums;

namespace KBot.Modules.ReactionRoles;

public class ReactionRoleComponents : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("rradd")]
    public async Task AddRrRoleAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var msg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var getRoleMsg = await Context.Channel.SendMessageAsync("Kérlek pingeld be a kívánt rangot").ConfigureAwait(false);

        var roleMsg = await InteractiveService.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id, timeout: TimeSpan.FromMinutes(2)).ConfigureAwait(false);
        if (!roleMsg.IsSuccess)
        {
            await Context.Channel.SendMessageAsync("Sajnos nem sikerült a rang megadása, kérlek próbáld újra").ConfigureAwait(false);
            await getRoleMsg.DeleteAsync().ConfigureAwait(false);
            return;
        }

        var roleId = Regex.Replace(roleMsg.Value!.Content, "[^0-9]", "");
        var role = Context.Guild.GetRole(Convert.ToUInt64(roleId));

        var getEmoteMsg = await Context.Channel.SendMessageAsync("Kérlek add meg a kívánt emote-ot").ConfigureAwait(false);
        var emojiMsg = await InteractiveService.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id, timeout: TimeSpan.FromMinutes(2)).ConfigureAwait(false);
        if (!emojiMsg.IsSuccess)
        {
            await Context.Channel.SendMessageAsync("Sajnos nem sikerült a kívánt emojit megadni, kérlek próbáld újra").ConfigureAwait(false);
            await getRoleMsg.DeleteAsync().ConfigureAwait(false);
            await getEmoteMsg.DeleteAsync().ConfigureAwait(false);
            return;
        }
        var emoteResult = Emote.TryParse(emojiMsg.Value!.Content, out var emote);
        var emojiResult = Emoji.TryParse(emojiMsg.Value!.Content, out var emoji);

        if (emojiResult)
        {
            embed.AddField(role.Name, $"{emoji} {role.Mention}");
        }
        else if (emoteResult)
        {
            embed.AddField(role.Name, $"{emote} {role.Mention}");
        }
        else
        {
            await Context.Channel.SendMessageAsync("Sajnos nem sikerült a kívánt emojit megadni, kérlek próbáld újra").ConfigureAwait(false);
            await getRoleMsg.DeleteAsync().ConfigureAwait(false);
            await getEmoteMsg.DeleteAsync().ConfigureAwait(false);
            return;
        }

        await getRoleMsg.DeleteAsync().ConfigureAwait(false);
        await roleMsg.Value.DeleteAsync().ConfigureAwait(false);
        await getEmoteMsg.DeleteAsync().ConfigureAwait(false);
        await emojiMsg.Value.DeleteAsync().ConfigureAwait(false);
        await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
    }
    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("rrremove")]
    public async Task RemoveRrRoleAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var msg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var getRoleMsg = await Context.Channel.SendMessageAsync("Kérlek pingeld be a kívánt rangot").ConfigureAwait(false);
        var roleMsg = await InteractiveService.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id).ConfigureAwait(false);
        if (!roleMsg.IsSuccess)
        {
            await Context.Channel.SendMessageAsync("Sajnos nem sikerült a rang megadása").ConfigureAwait(false);
            await getRoleMsg.DeleteAsync().ConfigureAwait(false);
            return;
        }

        var roleId = Regex.Replace(roleMsg.Value!.Content, "[^0-9]", "");
        EmbedFieldBuilder field;
        try
        {
            field = embed.Fields.First(x => ((string) x.Value).Contains($"<@&{roleId}>"));
        }
        catch (InvalidOperationException)
        {
            await Context.Channel.SendMessageAsync("Nincs ilyen rang a menüben, kérlek próbáld újra").ConfigureAwait(false);
            await getRoleMsg.DeleteAsync().ConfigureAwait(false);
            return;
        }
        embed.Fields.Remove(field);
        await getRoleMsg.DeleteAsync().ConfigureAwait(false);
        await roleMsg.Value.DeleteAsync().ConfigureAwait(false);
        await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
    }
    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("rrsave")]
    public async Task SaveRrRoleAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var msg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var components = new ComponentBuilder();
        foreach (var valu in embed.Fields.Select(field => field.Value as string))
        {
            var emoteResult = Emote.TryParse(valu!.Split(" ")[0], out var emote);
            var emojiResult = Emoji.TryParse(valu!.Split(" ")[0], out var emoji);
            var role = Context.Guild.GetRole(Convert.ToUInt64(valu!.Split(" ")[1].Replace("<@&", "").Replace(">", "")));
            if (emoteResult)
            {
                components.WithButton(role.Name, $"rrtr:{role.Id}", emote:emote);
            }
            else
            {
                components.WithButton(role.Name, $"rrtr:{role.Id}", emote:emoji);
            }
        }

        var fembed = new EmbedBuilder()
            .WithTitle("Reakciós Rangok")
            .WithDescription(embed.Description.Replace("Menü a reaction role-ok beállításához.\n", ""))
            .WithColor(Color.Gold)
            .Build();
        await Context.Channel.SendMessageAsync(embed: fembed, components: components.Build()).ConfigureAwait(false);
        await msg.DeleteAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("rrtr:*")]
    public async Task ReactionRoleTriggerAsync(string roleId)
    {
        await DeferAsync().ConfigureAwait(false);
        var role = Context.Guild.GetRole(Convert.ToUInt64(roleId));
        var user = Context.Guild.GetUser(Context.User.Id);
        if (user.Roles.Contains(role))
        {
            await user.RemoveRoleAsync(role).ConfigureAwait(false);
            await FollowupWithEmbedAsync(EmbedResult.Error, "Reakciós Rangok", $"Sikeresen levetted a {role.Mention} rangot", ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            await user.AddRoleAsync(role).ConfigureAwait(false);
            await FollowupWithEmbedAsync(EmbedResult.Success, "Reakciós Rangok", $"Sikeresen felvetted a {role.Mention} rangot", ephemeral: true).ConfigureAwait(false);
        }
    }
}