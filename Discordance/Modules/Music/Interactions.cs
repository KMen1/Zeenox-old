using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discordance.Enums;
using Discordance.Preconditions;

namespace Discordance.Modules.Music;

[RequirePlayer]
[RequireSameVoice]
[RequireDjRole]
[RequireSongRequester]
public class Interactions : MusicBase
{
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

    [ComponentInteraction("stop")]
    public async Task StopPlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await player.StopAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("volumeup")]
    public async Task IncreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        var newVolume = await SetVolumeAsync(player.Volume + 10 / 100f).ConfigureAwait(false);
        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Volume set to {newVolume.ToString(CultureInfo.InvariantCulture)}**"
                    )
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [ComponentInteraction("volumedown")]
    public async Task DecreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        var newVolume = await SetVolumeAsync(player.Volume - 10 / 100f).ConfigureAwait(false);
        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Volume set to {newVolume.ToString(CultureInfo.InvariantCulture)}**"
                    )
                    .WithColor(Color.Green)
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

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

    [ComponentInteraction("previous")]
    public async Task PlayPreviousAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await RewindAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("repeat")]
    public async Task ToggleRepeatAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await ToggleLoopAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("autoplay")]
    public async Task ToggleAutoplayAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await ToggleAutoPlayAsync().ConfigureAwait(false);
    }
}