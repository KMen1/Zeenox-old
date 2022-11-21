using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
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

    [SlashCommand("temp-create", "Create a new temporary channel hub")]
    public async Task SetupHubAsync(
        int userLimit,
        [Summary("Format", "Available: {user.name} {index}")]
        string nameFormat,
        [MinValue(8)] [MaxValue(96)] int bitrate
    )
    {
        var category = await Context.Guild.CreateCategoryChannelAsync("Rename me (Temp category)");
        var createChannel = await Context.Guild.CreateVoiceChannelAsync("Rename me (Create channel)", x =>
        {
            x.CategoryId = category.Id;
            x.Bitrate = bitrate * 1000;
            x.UserLimit = userLimit;
            x.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(
                new[]
                {
                    new Overwrite(
                        Context.Client.CurrentUser.Id,
                        PermissionTarget.User,
                        new OverwritePermissions(moveMembers: PermValue.Allow)
                    ),
                    new Overwrite(
                        Context.Guild.EveryoneRole.Id,
                        PermissionTarget.Role,
                        new OverwritePermissions(speak: PermValue.Deny, connect: PermValue.Allow,
                            moveMembers: PermValue.Deny, sendMessages: PermValue.Deny)
                    )
                });
        });

        await UpdateGuildConfigAsync(
                x =>
                    x.Hubs.Add(
                        new Hub(
                            category.Id,
                            createChannel.Id,
                            nameFormat,
                            userLimit,
                            bitrate * 1000
                        )
                    )
            )
            .ConfigureAwait(false);

        await RespondAsync("Successfully created a new temporary channel hub.", ephemeral: true)
            .ConfigureAwait(false);
    }

    [SlashCommand("temp-menu", "Change settings of your temporary channel")]
    public async Task SendTempMenuAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
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

        var isLocked = channel.PermissionOverwrites.Any(x =>
            x.TargetType == PermissionTarget.Role && x.TargetId == Context.Guild.EveryoneRole.Id &&
            x.Permissions.Connect == PermValue.Deny);

        var embed = new EmbedBuilder()
            .WithTitle(channel.Name)
            .AddField("🎙️ Bitrate", $"`{channel.Bitrate / 1000} kbps`", true)
            .AddField("👥 User limit", $"`{channel.UserLimit.ToString()}`", true)
            .AddField("🔒 Who can join?", isLocked ? "`Only moved`" : "`Everyone`", true)
            .WithColor(Color.Green)
            .Build();

        var buttons = new ComponentBuilder()
            .WithButton("Lock", "lock", ButtonStyle.Danger, new Emoji("🔒"))
            .WithButton("Ban", "ban", ButtonStyle.Danger, new Emoji("🚫"))
            .WithButton("Kick", "kick", ButtonStyle.Danger, new Emoji("👢"))
            .WithButton("Limit", "limit", ButtonStyle.Primary, new Emoji("👥"))
            .WithButton("Rename", "rename", ButtonStyle.Primary, new Emoji("📝"))
            .Build();

        await FollowupAsync(embed: embed, components: buttons).ConfigureAwait(false);
    }
}