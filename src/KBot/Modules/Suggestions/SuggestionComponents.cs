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
            .WithTitle($"Ötlet elfogadva {Context.User.Username} által")
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
            .WithTitle($"{Context.User.Username} Elfogadta az ötletedet!")
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
            .WithTitle($"Ötlet elutasítva {Context.User.Username} által")
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
            .WithTitle($"{Context.User.Username} Elutasította az ötletedet!")
            .WithDescription(embed.Description)
            .WithColor(Color.Red)
            .Build();
        var channel = await user.CreateDMChannelAsync().ConfigureAwait(false);
        await channel.SendMessageAsync(embed: userEb).ConfigureAwait(false);
    }
}