using Discord;
using Discord.Net;
using Discord.WebSocket;
using KBot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KBot.Services
{
    public class Command
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly VoiceCommands _voiceCommands;
        private readonly Help _help;

        public Command(IServiceProvider services)
        {
            _services = services;
            _client = services.GetRequiredService<DiscordSocketClient>();
            _help = new Help(_client);
            _voiceCommands = new VoiceCommands(_services);
        }

        public void InitializeAsync()
        {
            _client.SlashCommandExecuted += HandleSlashCommands;
            _client.Ready += CreateSlashCommands;
        }

        private async Task CreateSlashCommands()
        {
            await RegisterSlashCommands(await VoiceCommands.MakeVoiceCommands().ConfigureAwait(false));
        }

        public async Task RegisterSlashCommands(SlashCommandBuilder[] newCommands)
        {
            var globalCommands = await _client.GetGlobalApplicationCommandsAsync();

            List<string> existingCommandsName = new();
            foreach (var command in globalCommands)
            {
                existingCommandsName.Add(command.Name);
            }

            foreach (SlashCommandBuilder newCommand in newCommands)
            {
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
            };
        }

        public async Task HandleSlashCommands(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "help":
                    await _help.HandleHelpCommand(command);
                    break;
                case "join":
                    await _voiceCommands.Join(command);
                    break;
                case "leave":
                    await _voiceCommands.Leave(command);
                    break;
                case "play":
                    await _voiceCommands.Play(command);
                    break;
                case "stop":
                    await _voiceCommands.Stop(command);
                    break;
                case "move":
                    await _voiceCommands.Move(command);
                    break;
                case "skip":
                    await _voiceCommands.Skip(command);
                    break;
                case "pause":
                    await _voiceCommands.Pause(command);
                    break;
                case "resume":
                    await _voiceCommands.Resume(command);
                    break;
                case "volume":
                    await _voiceCommands.Volume(command);
                    break;
            }
        }
    }
}