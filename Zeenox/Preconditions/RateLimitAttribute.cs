using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Zeenox.Preconditions;

public class RateLimitAttribute : PreconditionAttribute
{
    public enum RateLimitType
    {
        User,
        Channel,
        Guild
    }

    private static readonly ConcurrentDictionary<ulong, List<RateLimitItem>> Items = new();
    private static DateTime _removeExpiredCommandsTime = DateTime.MinValue;
    private readonly int _requests;
    private readonly int _seconds;
    private readonly RateLimitType _type;

    public RateLimitAttribute(int seconds = 5, int requests = 1, RateLimitType type = RateLimitType.Guild)
    {
        _type = type;
        _requests = requests;
        _seconds = seconds;
    }

    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
        ICommandInfo commandInfo, IServiceProvider services)
    {
        if (DateTime.UtcNow > _removeExpiredCommandsTime)
            _ = Task.Run(async () =>
            {
                await ClearExpiredCommands().ConfigureAwait(false);
                _removeExpiredCommandsTime = DateTime.UtcNow.AddMinutes(30);
            });

        ulong id = _type switch
        {
            RateLimitType.User => context.User.Id,
            RateLimitType.Channel => context.Channel.Id,
            RateLimitType.Guild => context.Guild.Id,
            _ => 0
        };

        var dateTime = DateTime.UtcNow;
        var commandId = commandInfo.Module.Name + "//" + commandInfo.Name + "//" + commandInfo.MethodName;
        var target = Items.GetOrAdd(id, new List<RateLimitItem>());
        var matchingCommands = target.Where(
            a =>
                a.Id == commandId && dateTime >= a.ExpireAt
        ).ToList();

        foreach (var command in matchingCommands) target.Remove(command);

        if (target.Count(x => x.Id == commandId) >= _requests)
            return Task.FromResult(PreconditionResult.FromError(
                $"This command is usable <t:{((DateTimeOffset) target.Last().ExpireAt).ToUnixTimeSeconds()}:R>."));

        target.Add(new RateLimitItem(commandId, DateTime.UtcNow.AddSeconds(_seconds)));
        return Task.FromResult(PreconditionResult.FromSuccess());
    }

    private static Task ClearExpiredCommands()
    {
        foreach (var item in Items.Select(x => x.Value))
        {
            var utcTime = DateTime.UtcNow;
            foreach (var command in item.Where(a => utcTime > a.ExpireAt).ToList())
                item.Remove(command);
        }

        return Task.CompletedTask;
    }

    private sealed class RateLimitItem
    {
        public RateLimitItem(string id, DateTime expireAt)
        {
            Id = id;
            ExpireAt = expireAt;
        }

        public string Id { get; }
        public DateTime ExpireAt { get; }
    }
}