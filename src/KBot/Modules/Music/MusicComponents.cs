using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Enums;

namespace KBot.Modules.Music;

public class MusicComponents : SlashModuleBase
{
    public AudioService AudioService { get; set; }

    [ComponentInteraction("filterselectmenu")]
    public async Task ApplyFilterAsync(params string[] selections)
    {
        var result = Enum.TryParse(selections[0], out FilterType filterType);
        if (result)
        {
            await DeferAsync().ConfigureAwait(false);
            var embed = await AudioService.SetFiltersAsync(Context.Guild, Context.User, filterType)
                .ConfigureAwait(false);
            if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("stop")]
    public async Task StopPlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await AudioService.DisconnectAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("volumeup")]
    public async Task IncreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await AudioService.SetVolumeAsync(Context.Guild, Context.User, VoiceButtonType.VolumeUp)
            .ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("volumedown")]
    public async Task DecreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await AudioService.SetVolumeAsync(Context.Guild, Context.User, VoiceButtonType.VolumeDown)
            .ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("pause")]
    public async Task PausePlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await AudioService.PauseOrResumeAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("next")]
    public async Task SkipAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.PlayNextTrackAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }

    [ComponentInteraction("previous")]
    public async Task GoBackAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.PlayPreviousTrackAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }

    [ComponentInteraction("repeat")]
    public async Task ChangeLoopAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService.SetRepeatAsync(Context.Guild).ConfigureAwait(false);
    }

    [ComponentInteraction("clearfilters")]
    public async Task ClearFiltersAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await AudioService.ClearFiltersAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }
}