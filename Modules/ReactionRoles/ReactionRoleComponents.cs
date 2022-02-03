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
    [ComponentInteraction("rradd")]
    public async Task AddRrRoleAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var msg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var getRoleMsg = await Context.Channel.SendMessageAsync("Kérlek pingeld be a kívánt rangot").ConfigureAwait(false);

        var roleMsg = await InteractiveService.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id).ConfigureAwait(false);
        if (!roleMsg.IsSuccess)
        {
            await Context.Channel.SendMessageAsync("Sajnos nem sikerült a rang megadása").ConfigureAwait(false);
            return;
        }

        var roleId = Regex.Replace(roleMsg.Value!.Content, "[^0-9]", "");
        var role = Context.Guild.GetRole(Convert.ToUInt64(roleId));

        var getEmoteMsg = await Context.Channel.SendMessageAsync("Kérlek add meg a kívánt emote-ot").ConfigureAwait(false);
        var emojiMsg = await InteractiveService.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id).ConfigureAwait(false);
        if (!emojiMsg.IsSuccess)
        {
            await Context.Channel.SendMessageAsync("Sajnos nem sikerült a kívánt emojit megadni").ConfigureAwait(false);
            return;
        }
        var emote = Emote.Parse(emojiMsg.Value!.Content);

        embed.AddField(role.Name, $"{emote} {role.Mention}");

        await getRoleMsg.DeleteAsync().ConfigureAwait(false);
        await roleMsg.Value.DeleteAsync().ConfigureAwait(false);
        await getEmoteMsg.DeleteAsync().ConfigureAwait(false);
        await emojiMsg.Value.DeleteAsync().ConfigureAwait(false);
        await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
    }

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
            return;
        }

        var roleId = Regex.Replace(roleMsg.Value!.Content, "[^0-9]", "");
        var field = embed.Fields.First(x => ((string) x.Value).Contains($"<@&{roleId}>"));
        embed.Fields.Remove(field);

        await getRoleMsg.DeleteAsync().ConfigureAwait(false);
        await roleMsg.Value.DeleteAsync().ConfigureAwait(false);
        await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
    }

    [ComponentInteraction("rrsave")]
    public async Task SaveRrRoleAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var msg = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
        var embed = msg.Embeds.First().ToEmbedBuilder();

        var components = new ComponentBuilder();
        foreach (var field in embed.Fields)
        {
            var valu = field.Value as string;
            var role = Context.Guild.GetRole(Convert.ToUInt64(valu!.Split(" ")[1].Replace("<@&", "").Replace(">", "")));
            components.WithButton(role.Name, $"rrtr:{role.Id}", emote:Emote.Parse(valu.Split(" ")[0]));
        }

        var fembed = new EmbedBuilder()
            .WithTitle("Reakciós Rangok")
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