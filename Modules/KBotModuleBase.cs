using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using KBot.Database;
using KBot.Enums;
using KBot.Modules.Audio;
using Microsoft.Extensions.Caching.Memory;

namespace KBot.Modules;

public abstract class KBotModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    public DatabaseService Database { get; set; }
    public AudioService AudioService { get; set; }
    public InteractiveService InteractiveService { get; set; }
    public IMemoryCache Cache { get; set; }
    
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
        string url = null, string imageUrl = null, bool ephemeral = false)
    {
        var embed = new EmbedBuilder
        {
            Title = title,
            Description = description,
            Url = url,
            ImageUrl = imageUrl,
            Color = result == EmbedResult.Error ? Color.Red : Color.Green
        }.Build();
        return await Context.Interaction.FollowupAsync(embed: embed, ephemeral: ephemeral).ConfigureAwait(false);
    }

    protected async Task<GuildConfig> GetGuildConfigAsync()
    {
        return await Database.GetGuildConfigFromCacheAsync(Context.Guild.Id).ConfigureAwait(false);
    }
}