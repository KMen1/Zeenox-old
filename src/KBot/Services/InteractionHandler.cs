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
using KBot.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KBot.Services;

public class InteractionHandler : DiscordClientService
{
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;

    public InteractionHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger,
        IServiceProvider provider, InteractionService interactionService) : base(client, logger)
    {
        _provider = provider;
        _interactionService = interactionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.InteractionCreated += HandleInteractionAsync;
        Client.GuildAvailable += ClientOnGuildAvailableAsync;
        _interactionService.SlashCommandExecuted += HandleSlashCommandResultAsync;
        _interactionService.ComponentCommandExecuted += HandleComponentCommandResultAsync;
        try
        {
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to load modules");
        }

        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        await Client
            .SetGameAsync("/" + _provider.GetRequiredService<BotConfig>().Client.Game, type: ActivityType.Listening)
            .ConfigureAwait(false);
        await Client.SetStatusAsync(UserStatus.Online).ConfigureAwait(false);
    }

    private async Task ClientOnGuildAvailableAsync(SocketGuild arg)
    {
        try
        {
            await _interactionService.AddModulesToGuildAsync(arg, true, _interactionService.Modules.ToArray())
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Failed to load modules");
            throw;
        }
    }

    private static async Task HandleComponentCommandResultAsync(ComponentCommandInfo componentInfo,
        IInteractionContext interactionContext, IResult result)
    {
        if (result.IsSuccess) return;
        var interaction = interactionContext.Interaction;
        var eb = new EmbedBuilder()
            .WithAuthor("ERROR", "https://i.ibb.co/SrZZggy/x.png")
            .WithTitle("Please try again!")
            .WithColor(Color.Red)
            .AddField("Exception", $"```{result.ErrorReason}```")
            .Build();
        if (!interaction.HasResponded)
        {
            await interaction.RespondAsync(embed: eb).ConfigureAwait(false);
            return;
        }
        await interaction.FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    private static async Task HandleSlashCommandResultAsync(SlashCommandInfo commandInfo,
        IInteractionContext interactionContext,
        IResult result)
    {
        if (result.IsSuccess) return;

        var interaction = interactionContext.Interaction;
        var eb = new EmbedBuilder()
            .WithAuthor("ERROR", "https://i.ibb.co/SrZZggy/x.png")
            .WithTitle("Please try again!")
            .WithColor(Color.Red)
            .AddField("Exception", $"```{result.ErrorReason}```")
            .Build();
        if (!interaction.HasResponded)
        {
            await interaction.RespondAsync(embed: eb).ConfigureAwait(false);
            return;
        }
        await interaction.FollowupAsync(embed: eb).ConfigureAwait(false);
    }

    private async Task HandleInteractionAsync(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(Client, arg);
        await _interactionService.ExecuteCommandAsync(ctx, _provider).ConfigureAwait(false);
    }
}