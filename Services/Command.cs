using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using KBot.Commands;
using KBot.Commands.Voice;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace KBot.Services;

public class CommandService
{
    private readonly DiscordSocketClient _client;
    private readonly HelpCommand _help;
    private readonly HandleVoice _voiceCommands;

    public CommandService(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _help = new HelpCommand(_client);
        _voiceCommands = new HandleVoice(services);
    }

    public void InitializeAsync()
    {
        _client.SlashCommandExecuted += HandleSlashCommands;
        _client.Ready += CreateSlashCommands;
    }

    private async Task CreateSlashCommands()
    {
        await RegisterSlashCommands(await MakeVoice.MakeVoiceCommands().ConfigureAwait(false));
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

    private async Task HandleSlashCommands(SocketSlashCommand command)
    {
        await _voiceCommands.Handle(command);
    }
}