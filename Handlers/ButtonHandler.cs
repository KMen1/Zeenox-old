using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KBot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KBot.Handlers;

public class ButtonHandler
{
    private readonly DiscordSocketClient _client;
    private readonly AudioService _audioService;

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
        switch (arg.Data.CustomId)
        {
            case "stop":
                await _audioService.StopAsync(((ITextChannel) arg.Channel).Guild, arg.User);
                await _audioService.LeaveAsync(((ITextChannel) arg.Channel).Guild, ((IVoiceState)arg.User).VoiceChannel,
                    arg.User);
                await arg.Message.DeleteAsync();
                break;
            case "volumeup":
                var newEmbed = await _audioService.SetVolumeAsync(10, ((ITextChannel) arg.Channel).Guild, arg.User, true);
                await arg.UpdateAsync(x => x.Embed = newEmbed);
                break;
            case "volumedown":
                var newEmbed2 = await _audioService.SetVolumeAsync(10, ((ITextChannel) arg.Channel).Guild, arg.User, false, true);
                await arg.UpdateAsync(x => x.Embed = newEmbed2);
                break;
            case "pause":
                var (embed, buttons) =
                    await _audioService.PauseOrResumeAsync(((ITextChannel) arg.Channel).Guild, arg.User);
                await arg.UpdateAsync(x => x.Components = buttons);
                break;
            case "next":
                var (embed1, buttons1) = await _audioService.SkipAsync(((ITextChannel) arg.Channel).Guild, arg.User, true);
                await arg.UpdateAsync(x =>
                {
                    x.Embed = embed1;
                    x.Components = buttons1;
                });
                break;
            case "back":
                var (embed2, buttons2) = await _audioService.PlayPreviousTrack(((ITextChannel) arg.Channel).Guild, arg.User);
                await arg.UpdateAsync(x =>
                {
                    x.Embed = embed2;
                    x.Components = buttons2;
                });
                break;
        }
    }
}