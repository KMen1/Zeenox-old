﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using Zeenox.Extensions;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Modules;

public abstract class ModuleBase : InteractionModuleBase<ShardedInteractionContext>
{
    public MongoService DatabaseService { get; set; } = null!;
    public IMemoryCache Cache { get; set; } = null!;

    protected string GetLocalized(string key)
    {
        return Cache.GetMessage(Context.Guild.Id, key);
    }

    protected Embed GetLocalizedEmbed(string key, Color color)
    {
        return GetLocalized(key).ToEmbed(color);
    }

    protected Embed GetLocalizedEmbed(string key, Color color, object? object1)
    {
        return GetLocalized(key).Format(object1).ToEmbed(color);
    }

    protected Embed GetLocalizedEmbed(string key, Color color, object? object1, object? object2)
    {
        return GetLocalized(key).Format(object1, object2).ToEmbed(color);
    }

    protected Task<User> GetUserAsync(ulong? id = null)
    {
        return DatabaseService.GetUserAsync(id ?? Context.User.Id);
    }

    protected Task<User> UpdateUserAsync(Action<User> action, ulong? id = null)
    {
        return DatabaseService.UpdateUserAsync(id ?? Context.User.Id, action);
    }

    protected Task<GuildConfig> UpdateGuildConfigAsync(Action<GuildConfig> action)
    {
        return DatabaseService.UpdateGuildConfig(Context.Guild.Id, action);
    }
}