using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discordance.Models;
using Discordance.Services;

namespace Discordance.Modules.TemporaryChannels;

public class Commands : ModuleBase
{
    private readonly TemporaryChannelService _service;

    public Commands(TemporaryChannelService service)
    {
        _service = service;
    }

    [SlashCommand("temp-setup", "Setup a new temporary channel hub")]
    public async Task SetupHubAsync(
        ICategoryChannel category,
        IVoiceChannel createChannel,
        int userLimit,
        [Summary("NameFormat", "Available: {user.name} {index}")] string nameFormat,
        [MinValue(8), MaxValue(96)] int bitrate
    )
    {
        var config = await DatabaseService
            .GetGuildConfigAsync(Context.Guild.Id)
            .ConfigureAwait(false);

        if (config.TcHubs.Any(x => x.CategoryId == category.Id || x.ChannelId == createChannel.Id))
        {
            await RespondAsync(
                    "This category or channel is already used for a temporary channel hub.",
                    ephemeral: true
                )
                .ConfigureAwait(false);
            return;
        }

        await DatabaseService
            .UpdateGuildConfig(
                Context.Guild.Id,
                x =>
                    x.TcHubs.Add(
                        new Hub(category.Id, createChannel.Id, nameFormat, userLimit, bitrate * 1000)
                    )
            )
            .ConfigureAwait(false);

        await RespondAsync("Successfully created a new temporary channel hub.", ephemeral: true)
            .ConfigureAwait(false);
    }

    [SlashCommand("temp-ban", "Bans a user from a temporary voice channel")]
    public async Task BanUserAsync(SocketGuildUser user)
    {
        if (!_service.GetTempChannel(Context.User.Id, out var channelId))
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have a temporary channel!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var channel = Context.Guild.GetVoiceChannel(channelId);
        await channel
            .AddPermissionOverwriteAsync(user, OverwritePermissions.DenyAll(channel))
            .ConfigureAwait(false);

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"**{user.Mention}** has been banned from the temporary channel.")
            .Build();

        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("temp-unban", "Unbans a user from a temporary voice channel")]
    public async Task UnbanUserAsync(SocketGuildUser user)
    {
        if (!_service.GetTempChannel(Context.User.Id, out var channelId))
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have a temporary channel!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var channel = Context.Guild.GetVoiceChannel(channelId);
        await channel.RemovePermissionOverwriteAsync(user).ConfigureAwait(false);

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"**{user.Mention}** has been unbanned from the temporary channel.")
            .Build();

        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("temp-kick", "Kicks a user from a temporary voice channel")]
    public async Task KickUserAsync(SocketGuildUser user)
    {
        if (!_service.GetTempChannel(Context.User.Id, out _))
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have a temporary channel!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await user.ModifyAsync(x => x.Channel = null).ConfigureAwait(false);

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"**{user.Mention}** has been kicked from the temporary channel.")
            .Build();

        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("temp-limit", "Sets the limit of users in a temporary voice channel")]
    public async Task SetLimitAsync([MinValue(0), MaxValue(99)] int limit)
    {
        if (!_service.GetTempChannel(Context.User.Id, out var channelId))
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have a temporary channel!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var channel = Context.Guild.GetVoiceChannel(channelId);
        await channel.ModifyAsync(x => x.UserLimit = limit).ConfigureAwait(false);

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"**Limit set to {limit}**")
            .Build();

        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("temp-lock", "Locks a temporary voice channel")]
    public async Task LockChannelAsync()
    {
        if (!_service.GetTempChannel(Context.User.Id, out var channelId))
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have a temporary channel!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var channel = Context.Guild.GetVoiceChannel(channelId);
        await channel
            .AddPermissionOverwriteAsync(
                Context.Guild.EveryoneRole,
                new OverwritePermissions(connect: PermValue.Deny)
            )
            .ConfigureAwait(false);

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription("**Channel locked**")
            .Build();

        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("temp-unlock", "Unlocks a temporary voice channel")]
    public async Task UnlockChannelAsync()
    {
        if (!_service.GetTempChannel(Context.User.Id, out var channelId))
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have a temporary channel!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var channel = Context.Guild.GetVoiceChannel(channelId);
        await channel
            .AddPermissionOverwriteAsync(
                Context.Guild.EveryoneRole,
                new OverwritePermissions(connect: PermValue.Allow)
            )
            .ConfigureAwait(false);

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription("**Channel unlocked**")
            .Build();

        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("temp-name", "Renames a temporary voice channel")]
    public async Task RenameChannelAsync(string name)
    {
        if (!_service.GetTempChannel(Context.User.Id, out var channelId))
        {
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("**You don't have a temporary channel!**")
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var channel = Context.Guild.GetVoiceChannel(channelId);
        await channel.ModifyAsync(x => x.Name = name).ConfigureAwait(false);

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithDescription($"**Channel renamed to {name}**")
            .Build();

        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }
}
