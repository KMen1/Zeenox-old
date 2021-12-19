using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

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
        {
            await _interactionService.AddModulesToGuildAsync(guild, true, _interactionService.Modules.ToArray());
        }
    }
    private async Task HandleInteraction(SocketInteraction arg)
    {
        try
        {
            var ctx = new InteractionContext(_client, arg, arg.User, arg.Channel as ITextChannel);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception e)
        {
            if (arg.Type == InteractionType.ApplicationCommand)
                await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }
    private async Task RegisterSlashCommands(IEnumerable<SlashCommandBuilder> newCommands)
    {
        var globalCommands = await _client.GetGlobalApplicationCommandsAsync();

        var existingCommandsName = globalCommands.Select(command => command.Name).ToList();

        foreach (var newCommand in newCommands)
            //await _client.BulkOverwriteGlobalApplicationCommandsAsync(new ApplicationCommandProperties[] {newCommand.Build()});
            try
            {
                if (!existingCommandsName.Contains(newCommand.Name))
                    await _client.CreateGlobalApplicationCommandAsync(newCommand.Build());
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
    }
}