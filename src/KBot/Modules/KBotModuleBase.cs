using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using KBot.Enums;
using KBot.Models;
using KBot.Modules.Audio;
using KBot.Modules.DeadByDaylight;
using KBot.Services;
using Microsoft.Extensions.Caching.Memory;
using OsuSharp;
using OsuSharp.Interfaces;

namespace KBot.Modules;

public abstract class KBotModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    public DatabaseService Database { get; set; }
    public AudioService AudioService { get; set; }
    public InteractiveService InteractiveService { get; set; }
    public IMemoryCache Cache { get; set; }
    public OsuClient OsuClient { get; set; }
    public DbDService DbDService { get; set; }
    
    protected Task RespondWithEmbedAsync(EmbedResult result, string title, string description, string url = null,
        string imageUrl = null, bool ephemeral = false)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithUrl(url)
            .WithImageUrl(imageUrl)
            .WithColor(result == EmbedResult.Error ? Color.Red : Color.Green)
            .Build();
        return Context.Interaction.RespondAsync(embed: embed, ephemeral: ephemeral);
    }

    protected async Task<IUserMessage> FollowupWithEmbedAsync(EmbedResult result, string title, string description,
        string url = null, string imageUrl = null, bool ephemeral = false)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithUrl(url)
            .WithImageUrl(imageUrl)
            .WithColor(result == EmbedResult.Error ? Color.Red : Color.Green)
            .Build();
        return await Context.Interaction.FollowupAsync(embed: embed, ephemeral: ephemeral).ConfigureAwait(false);
    }

    protected async Task<GuildConfig> GetGuildConfigAsync()
    {
        return await Database.GetGuildConfigFromCacheAsync(Context.Guild.Id).ConfigureAwait(false);
    }
}