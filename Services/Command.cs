using Discord;
using Discord.Net;
using Discord.WebSocket;
using KBot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KBot.Services
{
    public class Command
    {
        private readonly DiscordSocketClient _client;
        private readonly VoiceCommands _voiceCommands;
        private readonly Help _help;

        public Command(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _help = new Help(_client);
            _voiceCommands = new VoiceCommands(services);
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

        private async Task RegisterSlashCommands(IEnumerable<SlashCommandBuilder> newCommands)
        {
            var globalCommands = await _client.GetGlobalApplicationCommandsAsync();

            var existingCommandsName = globalCommands.Select(command => command.Name).ToList();

            foreach (var newCommand in newCommands)
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
            }
        }

        private async Task HandleSlashCommands(SocketSlashCommand command)
        {
            switch (command.Data.Name.ToLower())
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
                case "loop":
                    await _voiceCommands.Loop(command);
                    break;
                case "bassboost":
                    await _voiceCommands.BassBoost(command);
                    break;
                case "nightcore":
                    await _voiceCommands.NightCore(command);
                    break;
                case "8d":
                    await _voiceCommands.EightD(command);
                    break;
                case "vaporwave":
                    await _voiceCommands.VaporWave(command);
                    break;
                case "karaoke":
                    await _voiceCommands.Karaoke(command);
                    break;
                case "clearfilters":
                    await _voiceCommands.ClearFilters(command);
                    break;
                /*case "speed":
                    await _voiceCommands.Speed(command);
                    break;
                case "pitch":
                    await _voiceCommands.Pitch(command);
                    break;*/
            }
        }
    }
}