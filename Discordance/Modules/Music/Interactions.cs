using System;
using System.Threading.Tasks;
using Discord.Interactions;
using Discordance.Enums;
using Discordance.Preconditions;

namespace Discordance.Modules.Music;

[RequirePlayer]
[RequireVoice]
[RequireDjRole]
public class Interactions : MusicBase
{
    
    [RequireSongRequester]
    [ComponentInteraction("filterselectmenu")]
    public async Task ApplyFilterAsync(params string[] selections)
    {
        var result = Enum.TryParse(selections[0], out FilterType filterType);
        if (result)
        {
            await DeferAsync().ConfigureAwait(false);
            await SetFilterAsync(filterType).ConfigureAwait(false);
        }
    }

    [RequireSongRequester]
    [ComponentInteraction("stop")]
    public async Task StopPlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await player.StopAsync().ConfigureAwait(false);
        player.IsAutoPlay = false;
        player.IsLooping = false;
    }

    [RequireSongRequester]
    [ComponentInteraction("volumeup")]
    public async Task IncreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await SetVolumeAsync(player.Volume + 10 / 100f).ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("volumedown")]
    public async Task DecreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await SetVolumeAsync(player.Volume - 10 / 100f).ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("pause")]
    public async Task PausePlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await PauseOrResumeAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("next")]
    public async Task PlayNextAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await SkipAsync().ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("previous")]
    public async Task PlayPreviousAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await RewindAsync().ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("repeat")]
    public async Task ToggleRepeatAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await ToggleLoopAsync().ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("autoplay")]
    public async Task ToggleAutoplayAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await ToggleAutoPlayAsync().ConfigureAwait(false);
    }
}