using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Serilog;
using Zeenox.Extensions;

namespace Zeenox.Services;

public class InteractionHandler : DiscordShardedClientService
{
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;

    public InteractionHandler(
        DiscordShardedClient client,
        ILogger<InteractionHandler> logger,
        IServiceProvider provider,
        InteractionService interactionService
    ) : base(client, logger)
    {
        _provider = provider;
        _interactionService = interactionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.InteractionCreated += HandleInteractionAsync;
        Client.ShardReady += ClientOnShardReadyAsync;
        _interactionService.SlashCommandExecuted += HandleSlashCommandResultAsync;
        _interactionService.ComponentCommandExecuted += HandleComponentCommandResultAsync;

        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        try
        {
            await _interactionService
                .AddModulesAsync(Assembly.GetEntryAssembly(), _provider)
                .ConfigureAwait(false);
            await _interactionService.AddModulesGloballyAsync(true, _interactionService.Modules.ToArray())
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Failed to add modules");
        }
    }

    private static async Task ClientOnShardReadyAsync(DiscordSocketClient client)
    {
        await client.SetGameAsync(
                "/" + Environment.GetEnvironmentVariable("BOT_ACTIVITY"),
                type: ActivityType.Listening
            )
            .ConfigureAwait(false);
        await client.SetStatusAsync(UserStatus.Online).ConfigureAwait(false);
    }

    private static async Task HandleComponentCommandResultAsync(
        ComponentCommandInfo componentInfo,
        IInteractionContext context,
        IResult result
    )
    {
        if (result.IsSuccess)
            return;

        var reason = result.Error switch
        {
            InteractionCommandError.UnmetPrecondition => result.ErrorReason,
            InteractionCommandError.UnknownCommand => "Unknown command, please restart your discord client",
            _ => "Something went wrong, please try again!"
        };

        var interaction = context.Interaction;

        if (!interaction.HasResponded)
        {
            await interaction.RespondAsync(embed: reason.ToEmbed(Color.Red), ephemeral: true).ConfigureAwait(false);
            return;
        }

        await interaction.FollowupAsync(embed: reason.ToEmbed(Color.Red), ephemeral: true).ConfigureAwait(false);
    }

    private static async Task HandleSlashCommandResultAsync(
        SlashCommandInfo commandInfo,
        IInteractionContext context,
        IResult result
    )
    {
        if (result.IsSuccess)
            return;

        var reason = result.Error switch
        {
            InteractionCommandError.UnmetPrecondition => result.ErrorReason,
            InteractionCommandError.UnknownCommand => "Unknown command, please restart your discord client",
            _ => "Something went wrong, please try again!"
        };

        var interaction = context.Interaction;

        if (!interaction.HasResponded)
        {
            await interaction.RespondAsync(embed: reason.ToEmbed(Color.Red), ephemeral: true).ConfigureAwait(false);
            return;
        }

        await interaction.FollowupAsync(embed: reason.ToEmbed(Color.Red), ephemeral: true).ConfigureAwait(false);
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var ctx = new ShardedInteractionContext(Client, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, _provider).ConfigureAwait(false);
    }
}