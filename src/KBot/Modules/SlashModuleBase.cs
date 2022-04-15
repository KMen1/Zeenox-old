using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Models.Guild;
using KBot.Models.User;
using KBot.Services;

namespace KBot.Modules;

public abstract class SlashModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    public DatabaseService Database { get; set; }
    public SocketUser BotUser => Context.Client.CurrentUser;

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

    protected ValueTask<User> GetDbUser(SocketUser user)
    {
        return Database.GetUserAsync(Context.Guild, user);
    }

    protected Task<User> UpdateUserAsync(SocketUser user, Action<User> action)
    {
        return Database.UpdateUserAsync(Context.Guild, user, action);
    }
}