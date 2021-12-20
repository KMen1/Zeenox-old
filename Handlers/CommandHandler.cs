using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace KBot.Handlers;

public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;

    public CommandHandler(IServiceProvider services)
    {
        _services = services;
        _client = services.GetRequiredService<DiscordSocketClient>();
        _interactionService = services.GetRequiredService<InteractionService>();
    }

    public async Task InitializeAsync()
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        _client.InteractionCreated += HandleInteraction;
        _client.Ready += GenerateSlashCommands;
    }

    private async Task GenerateSlashCommands()
    {
        foreach (var guild in _client.Guilds)
            await _interactionService.AddModulesToGuildAsync(guild, true, _interactionService.Modules.ToArray());
    }

    private async Task HandleInteraction(SocketInteraction arg)
    {
        try
        {
            var ctx = new InteractionContext(_client, arg, arg.User, arg.Channel as ITextChannel);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception)
        {
            if (arg.Type == InteractionType.ApplicationCommand)
                await arg.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
        }
    }
}