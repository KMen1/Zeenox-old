using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace KBot.Modules.Suggestions;

public class SuggestionComponents : KBotModuleBase
{
    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("suggest-accept:*")]
    public async Task AcceptSuggestionAsync(ulong userId)
    {
        var message = ((SocketMessageComponent) Context.Interaction).Message;
        var embed = message.Embeds.First();
        var eb = embed.ToEmbedBuilder()
            .AddField("Accepted by", Context.User.Mention)
            .WithColor(Color.Green)
            .Build();
        await message.ModifyAsync(x =>
        {
            x.Embed = eb;
            x.Components = null;
        }).ConfigureAwait(false);

        var user = await Context.Client.GetUserAsync(userId).ConfigureAwait(false);
        var userEb = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithTitle($"{Context.User.Username} accepted your suggestion!")
            .WithColor(Color.Green)
            .WithDescription(embed.Description)
            .Build();
        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        await channel.SendMessageAsync(embed: userEb).ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [ComponentInteraction("suggest-decline:*")]
    public async Task DeclineSuggestionAsync(ulong userId)
    {
        var message = ((SocketMessageComponent) Context.Interaction).Message;
        var embed = message.Embeds.First();
        var eb = embed.ToEmbedBuilder()
            .AddField("Denied by", Context.User.Mention)
            .WithColor(Color.Red)
            .Build();
        await message.ModifyAsync(x =>
        {
            x.Embed = eb;
            x.Components = null;
        }).ConfigureAwait(false);

        var user = await Context.Client.GetUserAsync(userId).ConfigureAwait(false);

        var userEb = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithTitle($"{Context.User.Username} denied your suggestion!")
            .WithDescription(embed.Description)
            .WithColor(Color.Red)
            .Build();
        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        await channel.SendMessageAsync(embed: userEb).ConfigureAwait(false);
    }
}