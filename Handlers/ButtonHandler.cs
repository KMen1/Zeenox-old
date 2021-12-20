using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KBot.Handlers;

public class ButtonHandler
{
    private readonly AudioService _audioService;
    private readonly DiscordSocketClient _client;

    public ButtonHandler(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _audioService = services.GetRequiredService<AudioService>();
    }

    public void InitializeAsync()
    {
        _client.ButtonExecuted += HandleButtonExecuted;
    }

    private async Task HandleButtonExecuted(SocketMessageComponent arg)
    {
        var guild = ((ITextChannel)arg.Channel).Guild;
        var user = arg.User;
        var voiceChannel = ((IVoiceState)user).VoiceChannel;

        Enum.TryParse(arg.Data.CustomId, true, out ButtonType buttonType);
        
        switch (buttonType)
        {
            case ButtonType.Stop:
                await HandleStopButton(guild, user, voiceChannel, arg);
                break;
            case ButtonType.VolumeUp:
                await HandleVolumeButtons(guild, user, arg, buttonType);
                break;
            case ButtonType.VolumeDown:
                await HandleVolumeButtons(guild, user, arg, buttonType);
                break;
            case ButtonType.Pause:
                await HadlePauseButton(guild, user, arg);
                break;
            case ButtonType.Next:
                await HandleForwardButton(guild, user, arg);
                break;
            case ButtonType.Previous:
                await HandleBackButton(guild, user, arg);
                break;
            case ButtonType.Repeat:
                await HandleRepeatButton(guild, user, arg);
                break;
        }
    }

    private async Task HandleRepeatButton(IGuild guild, SocketUser user, SocketMessageComponent interaction)
    {
        var (embed, buttons) = await _audioService.SetRepeatAsync(guild, user, interaction);
        await interaction.ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embed;
            x.Components = buttons;
        });
    }

    private async Task HandleBackButton(IGuild guild, SocketUser user, SocketMessageComponent interaction)
    {
        var (embed, buttons) = await _audioService.PlayPreviousTrack(guild, user);
        await interaction.UpdateAsync(x =>
        {
            x.Embed = embed;
            x.Components = buttons;
        });
    }

    private async Task HandleForwardButton(IGuild guild, SocketUser user, SocketMessageComponent interaction)
    {
        var (embed1, buttons1) = await _audioService.SkipAsync(guild, user, true);
        await interaction.UpdateAsync(x =>
        {
            x.Embed = embed1;
            x.Components = buttons1;
        });
    }

    private async Task HadlePauseButton(IGuild guild, SocketUser user, SocketMessageComponent interaction)
    {
        var (_, buttons) = await _audioService.PauseOrResumeAsync(guild, user);
        await interaction.UpdateAsync(x => x.Components = buttons);
    }

    private async Task HandleVolumeButtons(IGuild guild, SocketUser user, SocketMessageComponent interaction, ButtonType buttonType)
    {
        var newEmbed = await _audioService.SetVolumeAsync(0, guild, user, buttonType);
        await interaction.UpdateAsync(x => x.Embed = newEmbed);
    }

    private async Task HandleStopButton(IGuild guild, SocketUser user, IVoiceChannel voiceChannel, SocketMessageComponent interaction)
    {
        await _audioService.StopAsync(guild, user);
        await _audioService.LeaveAsync(guild, voiceChannel, user);
        await interaction.Message.DeleteAsync();
    }

}