using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KBot.Enums;

namespace KBot.Modules.Music;

public class MusicInteractions : SlashModuleBase
{
    private readonly MusicService _audioService;

    public MusicInteractions(MusicService audioService)
    {
        _audioService = audioService;
    }

    [ComponentInteraction("filterselectmenu")]
    public async Task ApplyFilterAsync(params string[] selections)
    {
        var result = Enum.TryParse(selections[0], out FilterTypes filterType);
        if (result)
        {
            await DeferAsync().ConfigureAwait(false);
            var embed = await _audioService.SetFiltersAsync(Context.Guild, Context.User, filterType)
                .ConfigureAwait(false);
            if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("stop")]
    public async Task StopPlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await _audioService.DisconnectAsync(Context.Guild, Context.User).ConfigureAwait(false);
        await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("volumeup")]
    public async Task IncreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await _audioService.SetVolumeAsync(Context.Guild, Context.User, VoiceButtonType.VolumeUp)
            .ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("volumedown")]
    public async Task DecreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await _audioService.SetVolumeAsync(Context.Guild, Context.User, VoiceButtonType.VolumeDown)
            .ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("pause")]
    public async Task PausePlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await _audioService.PauseOrResumeAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("next")]
    public async Task SkipAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await _audioService.PlayNextTrackAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }

    [ComponentInteraction("previous")]
    public async Task GoBackAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await _audioService.PlayPreviousTrackAsync(Context.Guild, Context.User).ConfigureAwait(false);
    }

    [ComponentInteraction("repeat")]
    public async Task ToggleLoopAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await _audioService.ToggleRepeatAsync(Context.Guild).ConfigureAwait(false);
    }

    [ComponentInteraction("autoplay")]
    public async Task ToggleAutoplayAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await _audioService.ToggleAutoplayAsync(Context.Guild).ConfigureAwait(false);
    }

    [ComponentInteraction("clearfilters")]
    public async Task ClearFiltersAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var embed = await _audioService.ClearFiltersAsync(Context.Guild, Context.User).ConfigureAwait(false);
        if (embed is not null) await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }
}