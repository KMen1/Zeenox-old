using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Enums;
using Discordance.Preconditions;

namespace Discordance.Modules.Music;

[RateLimit]
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
            await DeferAsync(true).ConfigureAwait(false);
            await SetFilterAsync(filterType).ConfigureAwait(false);
            var embed = filterType is FilterType.None
                ? GetLocalizedEmbed("set_filter_none", Color.Red)
                : GetLocalizedEmbed("set_filter_interact", Color.Green,
                    GetLocalized($"filter_{filterType.ToString().ToLower()}"));
            await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
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
        await DeferAsync(true).ConfigureAwait(false);
        var player = GetPlayer();
        await SetVolumeAsync(player.Volume + 10 / 100f).ConfigureAwait(false);
        await FollowupAsync(embed: GetLocalizedEmbed("set_volume", Color.Green, player.Volume * 100), ephemeral: true)
            .ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("volumedown")]
    public async Task DecreaseVolumeAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = GetPlayer();
        await SetVolumeAsync(player.Volume - 10 / 100f).ConfigureAwait(false);
        await FollowupAsync(embed: GetLocalizedEmbed("set_volume", Color.Green, player.Volume * 100), ephemeral: true)
            .ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("pause")]
    public async Task PausePlayerAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var isPaused = await PauseOrResumeAsync().ConfigureAwait(false);
        await FollowupAsync(embed: GetLocalizedEmbed(isPaused ? "pause_interact" : "resume_interact", Color.Green),
            ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("next")]
    public async Task PlayNextAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
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
        var isLooping = await ToggleLoopAsync().ConfigureAwait(false);
        await FollowupAsync(
            embed: GetLocalizedEmbed(isLooping ? "loop_interact_enabled" : "loop_interact_disabled", Color.Green),
            ephemeral: true).ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("autoplay")]
    public async Task ToggleAutoplayAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var isAutoplay = await ToggleAutoPlayAsync().ConfigureAwait(false);
        await FollowupAsync(
            embed: GetLocalizedEmbed(isAutoplay ? "autoplay_interact_enabled" : "autoplay_interact_disabled",
                Color.Green), ephemeral: true).ConfigureAwait(false);
    }
}