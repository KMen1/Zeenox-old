using System.Threading.Tasks;
using CloudinaryDotNet;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using KBot.Models;
using KBot.Modules.DeadByDaylight;
using KBot.Modules.Gambling;
using KBot.Modules.Music;
using KBot.Services;
using Microsoft.Extensions.Caching.Memory;
using OsuSharp;

namespace KBot.Modules;

public abstract class KBotModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    public DatabaseService Database { get; set; }
    public AudioService AudioService { get; set; }
    public InteractiveService InteractiveService { get; set; }
    public IMemoryCache Cache { get; set; }
    public OsuClient OsuClient { get; set; }
    public DbDService DbDService { get; set; }
    public GamblingService GamblingService { get; set; }
    public Cloudinary Cloudinary { get; set; }
    
    protected Task RespondWithEmbedAsync(Color color, string title, string description, string url = null,
        string imageUrl = null, bool ephemeral = false)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithUrl(url)
            .WithImageUrl(imageUrl)
            .WithColor(color)
            .Build();
        return Context.Interaction.RespondAsync(embed: embed, ephemeral: ephemeral);
    }

    protected async Task<IUserMessage> FollowupWithEmbedAsync(Color color, string title, string description,
        string url = null, string imageUrl = null, bool ephemeral = false)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithUrl(url)
            .WithImageUrl(imageUrl)
            .WithColor(color)
            .Build();
        return await Context.Interaction.FollowupAsync(embed: embed, ephemeral: ephemeral).ConfigureAwait(false);
    }

    protected Task<GuildConfig> GetGuildConfigAsync()
    {
        return Database.GetGuildConfigAsync(Context.Guild);
    }
}