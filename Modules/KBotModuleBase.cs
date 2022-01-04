using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using EmbedType = KBot.Enums.EmbedType;

namespace KBot.Modules;

public abstract class KBotModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    protected async Task RespondWithEmbedAsync(EmbedType type, string title, string description, string url = null, string imageUrl = null)
    {
        var embed = new EmbedBuilder
        {
            Title = title,
            Description = description,
            Url = url,
            ImageUrl = imageUrl,
            Color = type == EmbedType.Error ? Color.Red : Color.Green
        };
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }

    protected async Task<IUserMessage> FollowupWithEmbedAsync(EmbedType type, string title, string description, string url = null, string imageUrl = null)
    {
        var embed = new EmbedBuilder
        {
            Title = title,
            Description = description,
            Url = url,
            ImageUrl = imageUrl,
            Color = type == EmbedType.Error ? Color.Red : Color.Green
        };
        return await Context.Interaction.FollowupAsync(embed: embed.Build());
    }
}