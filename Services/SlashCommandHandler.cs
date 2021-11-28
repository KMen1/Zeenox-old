using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using KBot.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KBot.Services
{
    public class SlashCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly VoiceCommands _voiceCommands;
        private readonly Help _help;

        public SlashCommandHandler(IServiceProvider services)
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
            await _voiceCommands.MakeVoiceCommands();
        }

        public async Task HandleSlashCommands(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "help":
                    await _help.HandleHelpCommand(command);
                    break;
                case "join":
                    await _voiceCommands.HandleJoinCommand(command);
                    break;
                case "leave":
                    await _voiceCommands.HandleLeaveCommand(command);
                    break;
                case "play":
                    await _voiceCommands.HandlePlayCommand(command);
                    break;
                case "stop":
                    await _voiceCommands.HandleStopCommand(command);
                    break;
                case "move":
                    await _voiceCommands.HandleMoveCommand(command);
                    break;
                case "skip":
                    await _voiceCommands.HandleSkipCommand(command);
                    break;
                case "pause":
                    await _voiceCommands.HandlePauseCommand(command);
                    break;
                case "resume":
                    await _voiceCommands.HandleResumeCommand(command);
                    break;
                case "volume":
                    await _voiceCommands.HandleVolumeCommand(command);
                    break;
            }
        }

    }
}