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
using Lavalink4NET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Discordance.Services;

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
            //await _interactionService.AddModulesGloballyAsync(true, _interactionService.Modules.ToArray()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Failed to add modules");
        }

        //await _provider.GetRequiredService<IAudioService>().InitializeAsync().ConfigureAwait(false);
        _provider.GetRequiredService<PersistentRoleService>();
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
        IInteractionContext interactionContext,
        IResult result
    )
    {
        if (result.IsSuccess)
            return;
        EmbedBuilder eb;
        if (result.Error == InteractionCommandError.UnmetPrecondition)
        {
            eb = new EmbedBuilder()
                .WithDescription($"**{result.ErrorReason}**")
                .WithColor(Color.Red);
        }
        else
        {
            eb = new EmbedBuilder()
                .WithAuthor("Something went wrong", "https://i.ibb.co/SrZZggy/x.png")
                .WithTitle("Please try again!")
                .WithColor(Color.Red)
#if DEBUG
                .AddField("Exception", $"```{result.ErrorReason}```");
#endif
        }
        var interaction = interactionContext.Interaction;

        if (!interaction.HasResponded)
        {
            await interaction.RespondAsync(embed: eb.Build(), ephemeral: true).ConfigureAwait(false);
            return;
        }
        await interaction.FollowupAsync(embed: eb.Build(), ephemeral: true).ConfigureAwait(false);
    }

    private static async Task HandleSlashCommandResultAsync(
        SlashCommandInfo commandInfo,
        IInteractionContext interactionContext,
        IResult result
    )
    {
        if (result.IsSuccess)
            return;

        EmbedBuilder eb;
        if (result.Error == InteractionCommandError.UnmetPrecondition)
        {
            eb = new EmbedBuilder()
                .WithDescription($"**{result.ErrorReason}**")
                .WithColor(Color.Red);
        }
        else
        {
            eb = new EmbedBuilder()
                .WithAuthor("Something went wrong", "https://i.ibb.co/SrZZggy/x.png")
                .WithTitle("Please try again!")
                .WithColor(Color.Red)
#if DEBUG
                .AddField("Exception", $"```{result.ErrorReason}```");
#endif
        }
        var interaction = interactionContext.Interaction;

        if (!interaction.HasResponded)
        {
            await interaction.RespondAsync(embed: eb.Build(), ephemeral: true).ConfigureAwait(false);
            return;
        }
        await interaction.FollowupAsync(embed: eb.Build(), ephemeral: true).ConfigureAwait(false);
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var ctx = new ShardedInteractionContext(Client, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, _provider).ConfigureAwait(false);
    }
}
