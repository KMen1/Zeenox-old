using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KBot.Commands.Voice;

public class HandleVoice
{
    private readonly AudioService _audioService;

    public HandleVoice(IServiceProvider services)
    {
        _audioService = services.GetRequiredService<AudioService>();
    }

    public async Task Handle(SocketSlashCommand slashCommand)
    {
        Enum.TryParse(slashCommand.Data.Name.ToLower(), true, out VoiceCommandType commandType);
        if (slashCommand.Data.Name.ToLower() == "8d")
        {
            commandType = VoiceCommandType.EightD;
        }
        switch (commandType)
        {
            case VoiceCommandType.Join:
                await Join(slashCommand);
                break;
            case VoiceCommandType.Leave:
                await Leave(slashCommand);
                break;
            case VoiceCommandType.Move:
                await Move(slashCommand);
                break;
            case VoiceCommandType.Play:
                await Play(slashCommand);
                break;
            case VoiceCommandType.Stop:
                await Stop(slashCommand);
                break;
            case VoiceCommandType.Skip:
                await Skip(slashCommand);
                break;
            case VoiceCommandType.Pause:
                await Pause(slashCommand);
                break;
            case VoiceCommandType.Resume:
                await Resume(slashCommand);
                break;
            case VoiceCommandType.Volume:
                await Volume(slashCommand);
                break;
            case VoiceCommandType.Loop:
                await Loop(slashCommand);
                break;
            /*case VoiceCommandType.Fastforward:
                await FastForward(slashCommand);
                break;*/
            case VoiceCommandType.BassBoost:
                await BassBoost(slashCommand);
                break;
            case VoiceCommandType.NightCore:
                await NightCore(slashCommand);
                break;
            case VoiceCommandType.EightD:
                await EightD(slashCommand);
                break;
            case VoiceCommandType.Vaporwave:
                await VaporWave(slashCommand);
                break;
            case VoiceCommandType.Speed:
                await Speed(slashCommand);
                break;
            case VoiceCommandType.Pitch:
                await Pitch(slashCommand);
                break;
            case VoiceCommandType.ClearFilter:
                await ClearFilters(slashCommand);
                break;
            case VoiceCommandType.Queue:
                await Queue(slashCommand);
                break;
            case VoiceCommandType.ClearQueue:
                await ClearQueue(slashCommand);
                break;
        }
    }

    private async Task Join(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.JoinAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                ((IVoiceState) slashCommand.User).VoiceChannel,
                (ITextChannel) slashCommand.Channel,
                slashCommand.User));
    }

    private async Task Leave(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.LeaveAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                ((IVoiceState) slashCommand.User).VoiceChannel,
                slashCommand.User));
    }

    private async Task Play(SocketSlashCommand slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.PlayAsync(
                (string) slashCommand.Data.Options.First().Value,
                ((ITextChannel) slashCommand.Channel).Guild,
                ((IVoiceState) slashCommand.User).VoiceChannel,
                (ITextChannel) slashCommand.Channel,
                slashCommand.User));
    }

    private async Task Pause(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.PauseOrResumeAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                slashCommand.User));
    }

    private async Task Resume(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.PauseOrResumeAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                slashCommand.User));
    }

    private async Task Skip(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.SkipAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                slashCommand.User));
    }

    private async Task Stop(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.StopAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                slashCommand.User));
    }

    private async Task Move(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.MoveAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                ((IVoiceState) slashCommand.User).VoiceChannel,
                slashCommand.User));
    }

    private async Task Volume(SocketSlashCommand slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.SetVolumeAsync(
                Convert.ToUInt16(slashCommand.Data.Options.First().Value),
                ((ITextChannel) slashCommand.Channel).Guild,
                slashCommand.User));
    }

    private async Task Loop(SocketInteraction slashCommand)
    {
        await slashCommand.RespondAsync(embed:
            await _audioService.SetLoopAsync(
                ((ITextChannel) slashCommand.Channel).Guild,
                slashCommand.User));
    }

    private async Task BassBoost(SocketInteraction command)
    {
        await command.RespondAsync(embed:
            await _audioService.SetBassBoostAsync(
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }

    private async Task NightCore(SocketInteraction command)
    {
        await command.RespondAsync(embed:
            await _audioService.SetNightCoreAsync(
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }

    private async Task EightD(SocketInteraction command)
    {
        await command.RespondAsync(
            embed: await _audioService.SetEightDAsync(
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }

    private async Task VaporWave(SocketInteraction command)
    {
        await command.RespondAsync(embed:
            await _audioService.SetVaporWaveAsync(
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }

    private async Task Speed(SocketSlashCommand command)
    {
        await command.RespondAsync(embed:
            await _audioService.SetSpeedAsync(
                Convert.ToSingle(command.Data.Options.First().Value),
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }

    private async Task Pitch(SocketSlashCommand command)
    {
        await command.RespondAsync(embed:
            await _audioService.SetPitchAsync(
                Convert.ToSingle(command.Data.Options.First().Value),
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }

    private async Task ClearFilters(SocketInteraction command)
    {
        await command.RespondAsync(embed:
            await _audioService.ClearFiltersAsync(
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }
    
    private async Task ClearQueue(SocketInteraction command)
    {
        await command.RespondAsync(embed:
            await _audioService.ClearQueue(
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }

    private async Task Queue(SocketInteraction command)
    {
        await command.RespondAsync(embed:
            await _audioService.GetQueue(
                ((ITextChannel) command.Channel).Guild,
                command.User));
    }
}