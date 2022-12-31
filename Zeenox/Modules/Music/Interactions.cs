using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET.Player;
using Zeenox.Enums;
using Zeenox.Extensions;
using Zeenox.Preconditions;

namespace Zeenox.Modules.Music;

[RateLimit]
[RequirePlayer]
[RequireVoice]
[RequireDjRole]
public class Interactions : MusicBase
{
    [ComponentInteraction("favorite")]
    public async Task FavoriteSongAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = GetPlayer();

        var track = player.CurrentTrack;

        if (track is null)
        {
            await FollowupAsync("There is no song playing right now").ConfigureAwait(false);
            return;
        }

        var newUser = await UpdateUserAsync(x =>
        {
            if (x.Playlists[0].Songs.Contains(track.Identifier))
                x.Playlists[0].Songs.Remove(track.Identifier);
            else
                x.Playlists[0].Songs.Add(track.Identifier);
        }).ConfigureAwait(false);
        await FollowupAsync(newUser.Playlists[0].Songs.Contains(track.Identifier) ? "➕" : "➖", ephemeral: true)
            .ConfigureAwait(false);
    }

    [ComponentInteraction("filter")]
    public async Task SendFilterSelectAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var lang = Cache.GetLangKey(Context.Guild.Id);

        var selectMenu = new SelectMenuBuilder()
            .WithPlaceholder(Cache.GetMessage(lang, "FilterSelect"))
            .WithCustomId("filterselectmenu")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption(
                Cache.GetMessage(lang, "FilterNone"),
                "None",
                emote: new Emoji("🗑️")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterBassBoost"),
                "Bassboost",
                emote: new Emoji("🤔")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterPop"),
                "Pop",
                emote: new Emoji("🎸")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterSoft"),
                "Soft",
                emote: new Emoji("✨")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterTrebleBass"),
                "Treblebass",
                emote: new Emoji("🔊")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterNightCore"),
                "Nightcore",
                emote: new Emoji("🌃")
            )
            .AddOption(
                Cache.GetMessage(lang, "Filter8d"),
                "Eightd",
                emote: new Emoji("🎧")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterChinese"),
                "China",
                emote: new Emoji("🍊")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterVaporwave"),
                "Vaporwave",
                emote: new Emoji("💦")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterSpeedUp"),
                "Doubletime",
                emote: new Emoji("⏫")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterSpeedDown"),
                "Slowmotion",
                emote: new Emoji("⏬")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterChipmunk"),
                "Chipmunk",
                emote: new Emoji("🐿")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterDarthVader"),
                "Darthvader",
                emote: new Emoji("🤖")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterDance"),
                "Dance",
                emote: new Emoji("🕺")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterVibrato"),
                "Vibrato",
                emote: new Emoji("🕸")
            )
            .AddOption(
                Cache.GetMessage(lang, "FilterTremolo"),
                "Tremolo",
                emote: new Emoji("📳")
            );

        await FollowupAsync("Select a filter", ephemeral: true,
            components: new ComponentBuilder().WithSelectMenu(selectMenu).Build()).ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("filterselectmenu")]
    public async Task ApplyFilterAsync(params string[] selections)
    {
        var result = Enum.TryParse(selections[0], out FilterType filterType);
        if (result)
        {
            await DeferAsync(true).ConfigureAwait(false);
            await SetFilterAsync(filterType).ConfigureAwait(false);
            await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
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
        player.LoopMode = PlayerLoopMode.None;
    }

    [RequireSongRequester]
    [ComponentInteraction("volumeup")]
    public async Task IncreaseVolumeAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = GetPlayer();
        await SetVolumeAsync(player.Volume + 10 / 100f).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("volumedown")]
    public async Task DecreaseVolumeAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = GetPlayer();
        await SetVolumeAsync(player.Volume - 10 / 100f).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireSongRequester]
    [ComponentInteraction("pause")]
    public async Task PausePlayerAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await PauseOrResumeAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
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

    [ComponentInteraction("repeat")]
    public async Task ToggleRepeatAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await ToggleLoopAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }


    [RequireOwner]
    [ComponentInteraction("autoplay")]
    public async Task ToggleAutoplayAsync()
    {
        await DeferAsync().ConfigureAwait(false);
        await ToggleAutoPlayAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }
}