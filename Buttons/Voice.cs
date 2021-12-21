using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Services;

namespace KBot.Buttons;

public class Voice : InteractionModuleBase<InteractionContext>
{
    public AudioService AudioService { get; set; }
    
    [ComponentInteraction("stop")]
    public async Task Stop()
    {
        await AudioService.StopAsync(((ITextChannel) Context.Channel).Guild, (SocketUser)Context.User);
        await AudioService.LeaveAsync(((ITextChannel) Context.Channel).Guild, ((IVoiceState)Context.User).VoiceChannel, (SocketUser)Context.User);
        await ((SocketMessageComponent) Context.Interaction).Message.DeleteAsync();
    }
    [ComponentInteraction("volumeup")]
    public async Task VolumeUp()
    {
        var newEmbed = await AudioService.SetVolumeAsync(0, ((ITextChannel) Context.Channel).Guild, (SocketUser)Context.User, VoiceButtonType.VolumeUp);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x => x.Embed = newEmbed);
    }
    [ComponentInteraction("volumedown")]
    public async Task VolumeDown()
    {
        var newEmbed = await AudioService.SetVolumeAsync(0, ((ITextChannel) Context.Channel).Guild, (SocketUser)Context.User, VoiceButtonType.VolumeDown);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x => x.Embed = newEmbed);
    }

    [ComponentInteraction("pause")]
    public async Task Pause()
    {
        var (_, buttons) = await AudioService.PauseOrResumeAsync(((ITextChannel) Context.Channel).Guild, (SocketUser)Context.User);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x => x.Components = buttons);
    }
    [ComponentInteraction("next")]
    public async Task Next()
    {
        var (embed1, buttons1) = await AudioService.SkipAsync(((ITextChannel) Context.Channel).Guild, (SocketUser)Context.User, true);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x =>
        {
            x.Embed = embed1;
            x.Components = buttons1;
        });
    }
    [ComponentInteraction("previous")]
    public async Task Previous()
    {
        var (embed, buttons) = await AudioService.PlayPreviousTrack(((ITextChannel) Context.Channel).Guild, (SocketUser)Context.User);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x =>
        {
            x.Embed = embed;
            x.Components = buttons;
        });
    }
    [ComponentInteraction("repeat")]
    public async Task Repeat()
    {
        var (embed, buttons) = await AudioService.SetRepeatAsync(((ITextChannel) Context.Channel).Guild, (SocketUser)Context.User, (SocketMessageComponent)Context.Interaction);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x =>
        {
            x.Embed = embed;
            x.Components = buttons;
        });
    }
}