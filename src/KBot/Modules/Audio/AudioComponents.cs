using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Modules.Audio.Enums;

namespace KBot.Modules.Audio;

public class MusicComponents : KBotModuleBase
{
    [ComponentInteraction("filterselectmenu")]
    public async Task HandleFilterSelectMenu(params string[] selections)
    {
        var selection = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(selections[0].ToLower());

        var result = Enum.TryParse(selection, out FilterType filterType);
        if (result)
        {
            await DeferAsync().ConfigureAwait(false);
            await AudioService.SetFiltersAsync(Context.Guild, Context.User, filterType).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("stop")]
    public async Task Stop()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.DisconnectAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }
    [ComponentInteraction("volumeup")]
    public async Task VolumeUp()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.SetVolumeAsync(Context.Guild, Context.User, VoiceButtonType.VolumeUp).ConfigureAwait(false);
    }
    [ComponentInteraction("volumedown")]
    public async Task VolumeDown()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.SetVolumeAsync(Context.Guild, Context.User, VoiceButtonType.VolumeDown).ConfigureAwait(false);
    }

    [ComponentInteraction("pause")]
    public async Task Pause()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.PauseOrResumeAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }
    [ComponentInteraction("next")]
    public async Task Next()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.PlayNextTrackAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }
    [ComponentInteraction("previous")]
    public async Task Previous()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.PlayPreviousTrackAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }
    [ComponentInteraction("repeat")]
    public async Task Repeat()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.SetRepeatAsync(Context.Guild).ConfigureAwait(false);
    }

    [ComponentInteraction("clearfilters")]
    public async Task ClearFilters()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.ClearFiltersAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }
}