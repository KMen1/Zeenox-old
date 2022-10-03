using System;
using System.Threading.Tasks;
using Discord.Interactions;
using Discordance.Models;
using Discordance.Services;

namespace Discordance.Modules;

public abstract class ModuleBase : InteractionModuleBase<ShardedInteractionContext>
{
    private MongoService DatabaseService { get; set; } = null!;
    private LocalizationService Localization { get; set; } = null!;

    protected string GetLocalized(string key)
    {
        return Localization.GetMessage(Context.Guild.Id, key);
    }

    protected Task<User> GetUserAsync(ulong? id = null)
    {
        return DatabaseService.GetUserAsync(id ?? Context.User.Id);
    }

    protected Task UpdateUserAsync(Action<User> action, ulong? id = null)
    {
        return DatabaseService.UpdateUserAsync(id ?? Context.User.Id, action);
    }

    protected Task<GuildConfig> UpdateGuildConfigAsync(Action<GuildConfig> action)
    {
        return DatabaseService.UpdateGuildConfig(Context.Guild.Id, action);
    }
}