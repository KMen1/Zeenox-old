using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discordance.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace Discordance.Services;

public class PersistentRoleService
{
    private readonly IMemoryCache _cache;
    private readonly MongoService _mongo;

    public PersistentRoleService(
        DiscordShardedClient client,
        IMemoryCache cache,
        MongoService mongo
    )
    {
        _cache = cache;
        _mongo = mongo;
        client.UserJoined += UserJoinedAsync;
        client.GuildMemberUpdated += GuildMemberUpdatedAsync;
    }

    private async Task GuildMemberUpdatedAsync(
        Cacheable<SocketGuildUser, ulong> before,
        SocketGuildUser after
    )
    {
        var beforeUser = await before.GetOrDownloadAsync().ConfigureAwait(false);
        if (beforeUser is null)
            return;

        var config = _cache.GetGuildConfig(after.Guild.Id);
        if (!config.PersistentRoles)
            return;

        var rolesChanged = beforeUser.Roles
            .OrderBy(x => x)
            .SequenceEqual(after.Roles.OrderBy(x => x));
        if (!rolesChanged)
            return;

        await _mongo
            .UpdateGuildDataAsync(
                after,
                y => y.RoleIds = after.Roles.Where(z => !z.IsEveryone).Select(t => t.Id).ToList()
            )
            .ConfigureAwait(false);
    }

    private async Task UserJoinedAsync(SocketGuildUser user)
    {
        var config = _cache.GetGuildConfig(user.Guild.Id);
        if (!config.PersistentRoles)
            return;

        var userData = await _mongo.GetUserAsync(user.Id).ConfigureAwait(false);

        if (!userData.GuildDatas.ContainsKey(user.Guild.Id))
            return;

        foreach (
            var role in userData.GuildDatas[user.Guild.Id].RoleIds
                .Select(roleId => user.Guild.GetRole(roleId))
                .Where(role => role is not null)
        )
            await user.AddRoleAsync(role).ConfigureAwait(false);
    }
}