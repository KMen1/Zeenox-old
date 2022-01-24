using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KBot.Enums;

namespace KBot.Modules;

public abstract class KBotModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    protected Task RespondWithEmbedAsync(EmbedResult result, string title, string description, string url = null,
        string imageUrl = null)
    {
        var embed = new EmbedBuilder
        {
            Title = title,
            Description = description,
            Url = url,
            ImageUrl = imageUrl,
            Color = result == EmbedResult.Error ? Color.Red : Color.Green
        }.Build();
        return Context.Interaction.RespondAsync(embed: embed);
    }

    protected async Task<IUserMessage> FollowupWithEmbedAsync(EmbedResult result, string title, string description,
        string url = null, string imageUrl = null)
    {
        var embed = new EmbedBuilder
        {
            Title = title,
            Description = description,
            Url = url,
            ImageUrl = imageUrl,
            Color = result == EmbedResult.Error ? Color.Red : Color.Green
        }.Build();
        return await Context.Interaction.FollowupAsync(embed: embed).ConfigureAwait(false);
    }
}