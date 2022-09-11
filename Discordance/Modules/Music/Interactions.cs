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
            await AudioService
                .SetFiltersAsync(Context.Guild, Context.User, filterType)
                .ConfigureAwait(false);
        }
    }

    [ComponentInteraction("stop")]
    public async Task StopPlayerAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await player.StopAsync().ConfigureAwait(false);
        await player.WaitForInputAsync().ConfigureAwait(false);
    }

    [ComponentInteraction("volumeup")]
    public async Task IncreaseVolumeAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await player.SetVolumeAsync(Context.User, player.Volume + 10 / 100f).ConfigureAwait(false);
        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Volume set to {player.Volume.ToString(CultureInfo.InvariantCulture)}**"
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
        await player.SetVolumeAsync(Context.User, player.Volume - 10 / 100f).ConfigureAwait(false);
        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription(
                        $"**Volume set to {player.Volume.ToString(CultureInfo.InvariantCulture)}**"
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
        var embed = await AudioService
            .PauseOrResumeAsync(Context.Guild, Context.User)
            .ConfigureAwait(false);
        if (embed is not null)
            await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("next")]
    public async Task SkipAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await AudioService
            .SkipOrVoteskipAsync(Context.User, Context.Guild, Context.User.Id)
            .ConfigureAwait(false);
    }

    [ComponentInteraction("previous")]
    public async Task GoBackAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await player.PlayPreviousAsync(Context.User).ConfigureAwait(false);
    }

    [ComponentInteraction("repeat")]
    public async Task ToggleLoopAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await player.ToggleLoopAsync(Context.User).ConfigureAwait(false);
    }

    [ComponentInteraction("autoplay")]
    public async Task ToggleAutoplayAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = GetPlayer();
        await player.ToggleAutoPlayAsync(Context.User).ConfigureAwait(false);
    }
}
