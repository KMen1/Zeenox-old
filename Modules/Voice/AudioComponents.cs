using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;

namespace KBot.Modules.Voice;

public class Components : InteractionModuleBase<SocketInteractionContext>
{
    public AudioService AudioService { get; set; }

    [ComponentInteraction("filterselectmenu")]
    public async Task HandleFilterSelectMenu(params string[] selections)
    {
        var selection = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(selections[0].ToLower());

        var result = Enum.TryParse(selection, out FilterType filterType);
        if (result)
        {
            await DeferAsync().ConfigureAwait(false);
            await AudioService.SetFiltersAsync(Context.Guild, filterType).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("stop")]
    public async Task Stop()
    {
        await AudioService.StopAsync(Context.Guild).ConfigureAwait(false);
        await AudioService.DisconnectAsync(Context.Guild).ConfigureAwait(false);
        await ((SocketMessageComponent)Context.Interaction).Message.DeleteAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("volumeup")]
    public async Task VolumeUp()
    {
        await AudioService.SetVolumeAsync(Context.Guild, VoiceButtonType.VolumeUp).ConfigureAwait(false);
        await DeferAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("volumedown")]
    public async Task VolumeDown()
    {
        await AudioService.SetVolumeAsync(Context.Guild, VoiceButtonType.VolumeDown).ConfigureAwait(false);
        await DeferAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("pause")]
    public async Task Pause()
    {
        await AudioService.PauseOrResumeAsync(Context.Guild).ConfigureAwait(false);
        await DeferAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("next")]
    public async Task Next()
    {
        await AudioService.PlayNextTrackAsync(Context.Guild, Context.User).ConfigureAwait(false);
        await DeferAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("previous")]
    public async Task Previous()
    {
        await AudioService.PlayPreviousTrackAsync(Context.Guild).ConfigureAwait(false);
        await DeferAsync().ConfigureAwait(false);
    }
    [ComponentInteraction("repeat")]
    public async Task Repeat()
    {
        await AudioService.SetRepeatAsync(Context.Guild).ConfigureAwait(false);
        await DeferAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("clearfilters")]
    public async Task ClearFilters()
    {
        await AudioService.ClearFiltersAsync(Context.Guild).ConfigureAwait(false);
        await DeferAsync().ConfigureAwait(false);
    }
}