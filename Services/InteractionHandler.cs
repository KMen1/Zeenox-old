using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace KBot.Services;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;

    public InteractionHandler(IServiceProvider services)
    {
        _services = services;
        _client = services.GetRequiredService<DiscordSocketClient>();
        _interactionService = services.GetRequiredService<InteractionService>();
    }

    public async Task InitializeAsync()
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services).ConfigureAwait(false);
        _client.InteractionCreated += HandleInteractionAsync;
        _interactionService.SlashCommandExecuted += HandleSlashCommandResultAsync;
        _interactionService.ComponentCommandExecuted += HandleComponentCommandResultAsync;
        _client.Ready += GenerateSlashCommandsAsync;
    }

    private static async Task HandleComponentCommandResultAsync(ComponentCommandInfo componentInfo, IInteractionContext interactionContext, IResult result)
    {
        if (result.IsSuccess) return;
        var interaction = interactionContext.Interaction;

        switch (result.Error)
        {
            case InteractionCommandError.Exception:
            {
                await interaction.FollowupAsync(embed:
                    await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.ConvertFailed:
            {
                await interaction.FollowupAsync(embed:
                    await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.BadArgs:
            {
                await interaction.FollowupAsync(embed:
                    await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.Unsuccessful:
            {
                await interaction.FollowupAsync(embed:
                    await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.UnmetPrecondition:
            {
                await interaction.FollowupAsync(embed:
                    await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.ParseFailed:
            {
                await interaction.FollowupAsync(embed:
                    await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
        }
    }

    private static async Task HandleSlashCommandResultAsync(SlashCommandInfo commandInfo, IInteractionContext interactionContext,
        IResult result)
    {
        if (result.IsSuccess) return;

        var interaction = interactionContext.Interaction;

        switch (result.Error)
        {
            case InteractionCommandError.Exception:
            {
                await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.ConvertFailed:
            {
                await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.BadArgs:
            {
                await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.Unsuccessful:
            {
                await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.UnmetPrecondition:
            {
                await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
            case InteractionCommandError.ParseFailed:
            {
                await interaction.FollowupAsync(embed: await EmbedHelper.ErrorEmbed(result.ErrorReason).ConfigureAwait(false)).ConfigureAwait(false);
                break;
            }
        }
    }

    private async Task GenerateSlashCommandsAsync()
    {
        foreach (var guild in _client.Guilds)
            await _interactionService.AddModulesToGuildAsync(guild, true, _interactionService.Modules.ToArray()).ConfigureAwait(false);
    }

    private async Task HandleInteractionAsync(SocketInteraction arg)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, arg);
            await _interactionService.ExecuteCommandAsync(ctx, _services).ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (arg.Type == InteractionType.ApplicationCommand)
                await arg.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync().ConfigureAwait(false)).Unwrap().ConfigureAwait(false);
        }
    }
}